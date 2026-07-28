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
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = FarmClaim.Domain.Entities.RefreshToken;

namespace FarmClaim.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IJwtService jwtService,
            ILogger<LoginCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Login attempt: {Email}", request.Request.Email);

            var user = await _context.Users
                .Include(u => u.RefreshToken)
                .FirstOrDefaultAsync(u => u.Email == request.Request.Email.ToLower().Trim() && !u.IsDeleted, ct);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Request.Password);

            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // === Check user status ===
            switch (user.Status)
            {
                case UserStatus.PendingVerification:
                    _logger.LogWarning("Unverified user attempted login: {UserId}", user.Id);
                    throw new ForbiddenException(
                        "Your email is not verified yet. " +
                        "Please check your inbox for the OTP, or call /api/v1/Auth/resend-otp to get a new one.");

                case UserStatus.Suspended:
                    _logger.LogWarning("Suspended user attempted login: {UserId}", user.Id);
                    throw new ForbiddenException(
                        "Your account has been suspended. " +
                        $"Reason: {user.StatusChangeReason ?? "Not specified"}. " +
                        "Please contact support.");

                case UserStatus.Blocked:
                    _logger.LogWarning("Blocked user attempted login: {UserId}", user.Id);
                    throw new ForbiddenException(
                        "Your account has been permanently blocked. " +
                        $"Reason: {user.StatusChangeReason ?? "Not specified"}. " +
                        "Please contact support for review.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User logged in: {UserId}", user.Id);

            return await GenerateAuthResponseAsync(user, ct);
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user, CancellationToken ct)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshTokenValue);

            if (user.RefreshToken != null)
            {
                user.RefreshToken.IsRevoked = true;
                user.RefreshToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            var newRefreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(newRefreshTokenEntity, ct);
            await _context.SaveChangesAsync(ct);

            return new AuthResponseDto
            {
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

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}