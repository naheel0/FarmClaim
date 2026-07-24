using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, PasswordResetResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IApplicationDbContext context,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PasswordResetResponseDto> Handle(ResetPasswordCommand command, CancellationToken ct)
        {
            var email = command.Request.Email.ToLower().Trim();
            var rawToken = command.Request.Token.Trim();

            _logger.LogInformation("Password reset attempt for: {Email}", email);

            // 1. Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException("User not found.");

            // 2. Hash the provided token
            var providedHash = HashToken(rawToken);

            // 3. Find matching token in DB (hash comparison)
            var resetToken = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.TokenHash == providedHash)
                .FirstOrDefaultAsync(ct);

            if (resetToken == null)
                throw new ValidationException(new List<string> { "Invalid or unknown reset token." });

            // 4. Check if already used
            if (resetToken.UsedAt.HasValue)
            {
                _logger.LogWarning("Already-used reset token attempted for user {UserId}", user.Id);
                throw new ValidationException(new List<string>
                {
                    "This reset link has already been used. Please request a new one."
                });
            }

            // 5. Check expiry
            if (resetToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired reset token attempted for user {UserId}", user.Id);
                throw new ValidationException(new List<string>
                {
                    "This reset link has expired. Please request a new one."
                });
            }

            // 6. Update password
            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, command.Request.NewPassword);

            // 7. Mark token as used
            resetToken.UsedAt = DateTime.UtcNow;

            // 8. Revoke ALL active refresh tokens (force re-login everywhere)
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && !t.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
                t.ReasonRevoked = "Password reset";
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Password successfully reset for user {UserId} from IP {Ip}",
                user.Id, command.ClientIp ?? "unknown");

            return new PasswordResetResponseDto
            {
                Message = "Password reset successful. Please log in with your new password.",
                ExpiresAt = null
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}