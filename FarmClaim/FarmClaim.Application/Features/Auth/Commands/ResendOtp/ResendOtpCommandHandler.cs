using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.Commands.VerifyEmail;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Auth.Commands.ResendOtp
{
    public class ResendOtpCommandHandler : IRequestHandler<ResendOtpCommand, VerifyEmailResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly ILogger<ResendOtpCommandHandler> _logger;

        private const int OtpExpirationMinutes = 10;
        private const int ResendCooldownSeconds = 60; // 1 min between resends

        public ResendOtpCommandHandler(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            ILogger<ResendOtpCommandHandler> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        public async Task<VerifyEmailResponseDto> Handle(ResendOtpCommand cmd, CancellationToken ct)
        {
            var email = cmd.Request.Email.ToLower().Trim();

            // Always return generic success — don't reveal if email exists
            var genericResponse = new VerifyEmailResponseDto
            {
                Message = "If the email exists and is pending verification, a new OTP has been sent."
            };

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

            if (user == null)
            {
                _logger.LogWarning("Resend OTP requested for non-existent email: {Email}", email);
                return genericResponse;
            }

            if (user.Status != UserStatus.PendingVerification)
            {
                _logger.LogWarning("Resend OTP attempted for user {UserId} with status {Status}",
                    user.Id, user.Status);
                return genericResponse;
            }

            // Cooldown check — prevent OTP bombing
            var lastCode = await _context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (lastCode != null && (DateTime.UtcNow - lastCode.CreatedAt).TotalSeconds < ResendCooldownSeconds)
            {
                var waitSeconds = ResendCooldownSeconds - (int)(DateTime.UtcNow - lastCode.CreatedAt).TotalSeconds;
                throw new Common.Exceptions.ValidationException(new List<string>
                {
                    $"Please wait {waitSeconds} seconds before requesting a new OTP."
                });
            }

            // Invalidate previous unused codes
            var previousCodes = await _context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id && c.UsedAt == null)
                .ToListAsync(ct);
            foreach (var prev in previousCodes)
                prev.UsedAt = DateTime.UtcNow;

            // Generate new OTP
            var otp = GenerateOtp();
            var otpHash = HashCode(otp);

            var newCode = new EmailVerificationCode
            {
                UserId = user.Id,
                CodeHash = otpHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpirationMinutes),
                CreatedByIp = cmd.ClientIp
            };

            await _context.EmailVerificationCodes.AddAsync(newCode, ct);
            await _context.SaveChangesAsync(ct);

            // Send OTP email
            await _emailQueue.EnqueueEmailAsync(
                toEmail: user.Email,
                templateName: "EmailVerificationOtp",
                model: new EmailVerificationOtpModel
                {
                    UserName = $"{user.FirstName} {user.LastName}",
                    Otp = otp,
                    ExpirationMinutes = OtpExpirationMinutes
                });

            _logger.LogInformation("OTP resent for user {UserId}", user.Id);

            return new VerifyEmailResponseDto
            {
                Message = "A new OTP has been sent to your email."
            };
        }

        private static string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }
    }
}