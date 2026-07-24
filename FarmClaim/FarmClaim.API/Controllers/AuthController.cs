using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Auth.Commands.ForgotPassword;
using FarmClaim.Application.Features.Auth.Commands.Login;
using FarmClaim.Application.Features.Auth.Commands.Logout;
using FarmClaim.Application.Features.Auth.Commands.RefreshToken;
using FarmClaim.Application.Features.Auth.Commands.Register;
using FarmClaim.Application.Features.Auth.Commands.ResetPassword;
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

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _mediator.Send(new RegisterUserCommand(request));

            SetRefreshTokenCookie(result.RefreshToken, 7);

            return Ok(new
            {
                AccessToken = result.AccessToken,
                ExpiresIn = result.ExpiresIn,
                User = result.User,
                Message = "Registration successful."
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
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

        [HttpPost("refresh")]
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
        }

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