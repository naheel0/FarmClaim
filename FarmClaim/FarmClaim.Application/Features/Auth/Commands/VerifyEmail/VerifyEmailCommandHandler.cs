using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using RefreshTokenEntity = FarmClaim.Domain.Entities.RefreshToken;

namespace FarmClaim.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<VerifyEmailCommandHandler> _logger;

        public VerifyEmailCommandHandler(
            IApplicationDbContext context,
            IJwtService jwtService,
            ILogger<VerifyEmailCommandHandler> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<VerifyEmailResponseDto> Handle(VerifyEmailCommand cmd, CancellationToken ct)
        {
            var email = cmd.Request.Email.ToLower().Trim();
            var otp = cmd.Request.Otp.Trim();

            _logger.LogInformation("Email verification attempt for: {Email}", email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException("User not found.");

            if (user.Status == UserStatus.Active)
                throw new ValidationException(new List<string> { "Email is already verified." });

            if (user.Status != UserStatus.PendingVerification)
                throw new ForbiddenException("Account is not in a verifiable state.");

            // Find the latest unused code for this user
            var code = await _context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id && c.UsedAt == null)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (code == null)
                throw new ValidationException(new List<string>
                {
                    "No active verification code found. Please request a new OTP."
                });

            // Check if locked due to too many attempts
            if (code.IsLocked)
            {
                _logger.LogWarning("OTP locked for user {UserId} after 3 failed attempts", user.Id);
                throw new ValidationException(new List<string>
                {
                    "Too many incorrect attempts. Please request a new OTP."
                });
            }

            // Check expiry
            if (code.IsExpired)
                throw new ValidationException(new List<string>
                {
                    "OTP has expired. Please request a new one."
                });

            // Hash the provided OTP and compare
            var providedHash = HashCode(otp);

            if (code.CodeHash != providedHash)
            {
                code.AttemptCount++;
                await _context.SaveChangesAsync(ct);

                var remaining = 3 - code.AttemptCount;
                throw new ValidationException(new List<string>
                {
                    remaining > 0
                        ? $"Invalid OTP. {remaining} attempt(s) remaining."
                        : "Too many incorrect attempts. Please request a new OTP."
                });
            }

            // Success — activate user, mark code as used
            user.Status = UserStatus.Active;
            user.LastLoginAt = DateTime.UtcNow;
            code.UsedAt = DateTime.UtcNow;

            // Generate tokens (auto-login after verification)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshTokenValue);

            var refreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Email verified for user {UserId} from IP {Ip}",
                user.Id, cmd.ClientIp ?? "unknown");

            return new VerifyEmailResponseDto
            {
                Message = "Email verified successfully. You are now logged in.",
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresIn = _jwtService.AccessTokenExpirationMinutes * 60,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber
                }
            };
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}