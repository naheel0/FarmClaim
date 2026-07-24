using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Auth.Commands.ConfirmEmailChange
{
    public class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, EmailChangeResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ConfirmEmailChangeCommandHandler> _logger;

        public ConfirmEmailChangeCommandHandler(
            IApplicationDbContext context,
            ILogger<ConfirmEmailChangeCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailChangeResponseDto> Handle(ConfirmEmailChangeCommand cmd, CancellationToken ct)
        {
            var newEmail = cmd.Request.NewEmail.ToLower().Trim();
            var rawToken = cmd.Request.Token.Trim();

            // 1. Hash the provided token
            var providedHash = HashToken(rawToken);

            // 2. Find matching token
            var token = await _context.EmailChangeTokens
                .Include(t => t.User)
                .Where(t => t.TokenHash == providedHash
                            && t.NewEmail == newEmail
                            && t.UsedAt == null)
                .FirstOrDefaultAsync(ct);

            if (token == null)
                throw new ValidationException(new List<string> { "Invalid or unknown verification token." });

            if (token.ExpiresAt <= DateTime.UtcNow)
                throw new ValidationException(new List<string> { "This verification link has expired. Please request a new one." });

            var user = token.User;
            if (user == null || user.IsDeleted)
                throw new NotFoundException(nameof(User), token.UserId);

            // 3. Re-check that the new email isn't taken (race condition safety)
            var emailTaken = await _context.Users
                .AnyAsync(u => u.Id != user.Id && u.Email == newEmail && !u.IsDeleted, ct);

            if (emailTaken)
                throw new ValidationException(new List<string> { "This email is now in use by another account." });

            // 4. Capture old email for notification
            var oldEmail = user.Email;

            // 5. Update user's email
            user.Email = newEmail;
            token.UsedAt = DateTime.UtcNow;

            // 6. Revoke ALL active refresh tokens (force re-login)
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && !t.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
                t.ReasonRevoked = "Email changed";
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Email changed for user {UserId}: {OldEmail} → {NewEmail} from IP {Ip}",
                user.Id, oldEmail, newEmail, cmd.ClientIp ?? "unknown");

            return new EmailChangeResponseDto
            {
                Message = $"Email successfully changed from {oldEmail} to {newEmail}. Please log in again with your new email."
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}