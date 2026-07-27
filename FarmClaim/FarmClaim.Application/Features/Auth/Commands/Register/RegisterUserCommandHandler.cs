using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.Commands.VerifyEmail;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace FarmClaim.Application.Features.Auth.Commands.Register
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        private const int OtpExpirationMinutes = 10;

        public RegisterUserCommandHandler(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RegisterResponseDto> Handle(RegisterUserCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Registering user: {Email}", request.Request.Email);

            var exists = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Request.Email.ToLower(), ct);

            if (exists != null)
                throw new InvalidOperationException($"Email '{request.Request.Email}' already registered.");

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

            var user = new User
            {
                Email = request.Request.Email.ToLower().Trim(),
                FirstName = request.Request.FirstName.Trim(),
                LastName = request.Request.LastName.Trim(),
                PhoneNumber = request.Request.PhoneNumber?.Trim(),
                Role = UserRole.Farmer,
                Status = UserStatus.PendingVerification   // ← NEW: requires OTP verification
            };

            user.PasswordHash = hasher.HashPassword(user, request.Request.Password);

            await _context.Users.AddAsync(user, ct);
            await _context.SaveChangesAsync(ct);

            // Generate 6-digit OTP
            var otp = GenerateOtp();
            var otpHash = HashCode(otp);

            // Invalidate previous codes for this user (if any)
            var previousCodes = await _context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id && c.UsedAt == null)
                .ToListAsync(ct);
            foreach (var prev in previousCodes)
                prev.UsedAt = DateTime.UtcNow;

            var verificationCode = new EmailVerificationCode
            {
                UserId = user.Id,
                CodeHash = otpHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpirationMinutes)
            };

            await _context.EmailVerificationCodes.AddAsync(verificationCode, ct);
            await _context.SaveChangesAsync(ct);

            // Send OTP email (fire-and-forget via Hangfire)
            await _emailQueue.EnqueueEmailAsync(
                toEmail: user.Email,
                templateName: "EmailVerificationOtp",
                model: new EmailVerificationOtpModel
                {
                    UserName = $"{user.FirstName} {user.LastName}",
                    Otp = otp,
                    ExpirationMinutes = OtpExpirationMinutes
                });

            _logger.LogInformation("User registered (pending verification): {UserId}, OTP sent", user.Id);

            return new RegisterResponseDto
            {
                Message = "Account created successfully. Please verify your email using the OTP sent to your inbox.",
                UserId = user.Id,
                Email = user.Email,
                RequiresEmailVerification = true,
                OtpExpiresAt = verificationCode.ExpiresAt
            };
        }

        private static string GenerateOtp()
        {
            // Cryptographically secure 6-digit code
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }
    }
}