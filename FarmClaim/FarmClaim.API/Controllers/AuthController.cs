using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Auth.Commands.ChangeEmail;
using FarmClaim.Application.Features.Auth.Commands.ConfirmEmailChange;
using FarmClaim.Application.Features.Auth.Commands.ForgotPassword;
using FarmClaim.Application.Features.Auth.Commands.Login;
using FarmClaim.Application.Features.Auth.Commands.Logout;
using FarmClaim.Application.Features.Auth.Commands.RefreshToken;
using FarmClaim.Application.Features.Auth.Commands.Register;
using FarmClaim.Application.Features.Auth.Commands.ResendOtp;
using FarmClaim.Application.Features.Auth.Commands.ResetPassword;
using FarmClaim.Application.Features.Auth.Commands.VerifyEmail;
using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IMediator mediator, ILogger<AuthController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // REGISTER — Create account (requires email verification)
        // ============================================
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _mediator.Send(new RegisterUserCommand(request));
                // Don't set refresh token cookie — user must verify email first
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ============================================
        // VERIFY EMAIL — Submit OTP after registration
        // ============================================
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VerifyEmailResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new VerifyEmailCommand(request, clientIp);
                var result = await _mediator.Send(command);

                // Set refresh token cookie on successful verification (auto-login)
                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    SetRefreshTokenCookie(result.RefreshToken, 7);
                }

                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification failed for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ============================================
        // RESEND OTP — Request new verification code
        // ============================================
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VerifyEmailResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new ResendOtpCommand(request, clientIp);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resend OTP failed for {Email}", request.Email);
                return Ok(new VerifyEmailResponseDto
                {
                    Message = "If the email exists and is pending verification, a new OTP has been sent."
                });
            }
        }

        // ============================================
        // LOGIN — Authenticate with email + password
        // ============================================
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _mediator.Send(new LoginCommand(request));
                SetRefreshTokenCookie(result.RefreshToken, 7);

                return Ok(new
                {
                    AccessToken = result.AccessToken,
                    ExpiresIn = result.ExpiresIn,
                    User = result.User,
                    Message = "Login successful."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ============================================
        // REFRESH — Rotate tokens via cookie
        // ============================================
        [HttpPost("refresh")]
        [AllowAnonymous] // M15 FIX: Must be accessible without valid access token — caller's token is expired
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { error = "No refresh token cookie found." });
            }

            var accessToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "") ?? "";

            try
            {
                var result = await _mediator.Send(new RefreshTokenCommand(accessToken, refreshToken));
                SetRefreshTokenCookie(result.RefreshToken, 7);

                return Ok(new
                {
                    AccessToken = result.AccessToken,
                    ExpiresIn = result.ExpiresIn,
                    User = result.User
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                DeleteRefreshTokenCookie();
                return Unauthorized(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                DeleteRefreshTokenCookie();
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // ============================================
        // LOGOUT — Revoke refresh token
        // ============================================
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _mediator.Send(new LogoutCommand(userId));
            }

            DeleteRefreshTokenCookie();

            return Ok(new { Message = "Logged out successfully." });
        }

        // ============================================
        // FORGOT PASSWORD — Send reset email
        // ============================================
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PasswordResetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new ForgotPasswordCommand(request, clientIp);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed for {Email}", request.Email);
                // SECURITY: Never expose internal errors here
                return Ok(new PasswordResetResponseDto
                {
                    Message = "If the email exists in our system, a reset link has been sent."
                });
            }
        }

        // ============================================
        // RESET PASSWORD — Submit new password
        // ============================================
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PasswordResetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new ResetPasswordCommand(request, clientIp);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset password failed for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        // ============================================
        // CHANGE EMAIL — Request email change (authenticated)
        // ============================================
        [HttpPost("change-email")]
        [Authorize]
        [ProducesResponseType(typeof(EmailChangeResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Invalid user identity." });

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new ChangeEmailCommand(userId, request, clientIp);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change email failed");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ============================================
        // CONFIRM EMAIL CHANGE — Verify token and apply change (anonymous)
        // ============================================
        [HttpPost("confirm-email-change")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EmailChangeResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var command = new ConfirmEmailChangeCommand(request, clientIp);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirm email change failed");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ============================================
        // PRIVATE HELPERS
        // ============================================
        private void SetRefreshTokenCookie(string token, int expiresInDays)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(expiresInDays),
                IsEssential = true
            });
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Append("refreshToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });
        }
    }
}