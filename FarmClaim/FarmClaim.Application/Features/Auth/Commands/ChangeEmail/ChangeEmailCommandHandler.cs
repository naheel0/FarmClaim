using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Auth.Commands.ChangeEmail
{
    public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, EmailChangeResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChangeEmailCommandHandler> _logger;

        private const int TokenExpirationMinutes = 60; // 1 hour to confirm

        public ChangeEmailCommandHandler(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            IConfiguration configuration,
            ILogger<ChangeEmailCommandHandler> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EmailChangeResponseDto> Handle(ChangeEmailCommand cmd, CancellationToken ct)
        {
            var newEmail = cmd.Request.NewEmail.ToLower().Trim();

            _logger.LogInformation("User {UserId} requesting email change to {NewEmail}",
                cmd.UserId, newEmail);

            // 1. Load user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == cmd.UserId && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException(nameof(User), cmd.UserId);

            if (user.Status != UserStatus.Active)
                throw new ForbiddenException("Account is not active. Email change not allowed.");

            // 2. Verify current password
            var hasher = new PasswordHasher<User>();
            var pwdResult = hasher.VerifyHashedPassword(user, user.PasswordHash, cmd.Request.CurrentPassword);

            if (pwdResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Current password is incorrect.");

            // 3. New email must differ from current
            if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException(new List<string> { "New email is the same as current email." });

            // 4. New email must not be in use
            var emailInUse = await _context.Users
                .AnyAsync(u => u.Email == newEmail && !u.IsDeleted, ct);

            if (emailInUse)
                throw new ValidationException(new List<string> { "This email is already registered to another account." });

            // 5. Invalidate previous pending tokens for this user
            var previousTokens = await _context.EmailChangeTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
            foreach (var prev in previousTokens)
                prev.UsedAt = DateTime.UtcNow;

            // 6. Generate token (raw → sent to NEW email; hash → stored)
            var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Base64UrlEncode(rawTokenBytes);
            var tokenHash = HashToken(rawToken);

            var token = new EmailChangeToken
            {
                UserId = user.Id,
                NewEmail = newEmail,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
                CreatedByIp = cmd.ClientIp
            };

            await _context.EmailChangeTokens.AddAsync(token, ct);
            await _context.SaveChangesAsync(ct);

            // 7. Send verification email to NEW address
            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";

            await _emailQueue.EnqueueEmailAsync(
                toEmail: newEmail,
                templateName: "EmailChangeConfirmation",
                model: new ChangeEmailTemplateModel
                {
                    UserName = $"{user.FirstName} {user.LastName}",
                    NewEmail = newEmail,
                    Token = rawToken,
                    FrontendBaseUrl = frontendBaseUrl
                });

            _logger.LogInformation("Email change verification sent to {NewEmail} for user {UserId}",
                newEmail, user.Id);

            return new EmailChangeResponseDto
            {
                Message = $"Verification email sent to {newEmail}. Please check your inbox and confirm within 1 hour.",
                ExpiresAt = token.ExpiresAt
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
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