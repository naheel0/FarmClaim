using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Models.Email;

namespace FarmClaim.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, PasswordResetResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        private const int TokenExpirationMinutes = 30;
        private const int TokenByteLength = 32; // 256-bit token

        public ForgotPasswordCommandHandler(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            IConfiguration configuration,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PasswordResetResponseDto> Handle(ForgotPasswordCommand command, CancellationToken ct)
        {
            var email = command.Request.Email.ToLower().Trim();
            _logger.LogInformation("Password reset requested for: {Email}", email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

            // SECURITY: Always return generic success — don't reveal if email exists
            // This prevents email enumeration attacks
            var genericResponse = new PasswordResetResponseDto
            {
                Message = "If the email exists in our system, a reset link has been sent.",
                ExpiresAt = null
            };

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                return genericResponse;
            }

            // SECURITY: Don't allow reset for blocked/suspended users
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Password reset attempted for non-active user {UserId} (Status={Status})",
                    user.Id, user.Status);
                return genericResponse;
            }

            // Generate raw token (sent to user via email)
            var rawTokenBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
            var rawToken = Base64UrlEncode(rawTokenBytes);

            // Hash the token for storage (NEVER store raw token)
            var tokenHash = HashToken(rawToken);

            // Invalidate all previous unused tokens for this user
            var previousTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
            foreach (var prev in previousTokens)
            {
                prev.UsedAt = DateTime.UtcNow;
            }

            // Create new token
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
                CreatedByIp = command.ClientIp
            };

            await _context.PasswordResetTokens.AddAsync(resetToken, ct);
            await _context.SaveChangesAsync(ct);

            // Enqueue email (fire-and-forget via Hangfire)
            var frontendBaseUrl = _configuration["FrontendBaseUrl"]
                ?? "http://localhost:3000";

            await _emailQueue.EnqueueEmailAsync(
                toEmail: user.Email,
                templateName: "PasswordResetEmail",
                model: new PasswordResetEmailModel
                {
                    Email = user.Email,
                    Token = rawToken,
                    FrontendBaseUrl = frontendBaseUrl
                });

            _logger.LogInformation("Password reset email queued for user {UserId}", user.Id);

            // SECURITY: Return same ExpiresAt as the non-existent email path to prevent enumeration
            return new PasswordResetResponseDto
            {
                Message = "If the email exists in our system, a reset link has been sent.",
                ExpiresAt = null
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes); // 64-char hex string
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}