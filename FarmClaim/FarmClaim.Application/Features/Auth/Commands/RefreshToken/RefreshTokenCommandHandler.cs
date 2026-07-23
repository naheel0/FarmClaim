using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = FarmClaim.Domain.Entities.RefreshToken;

namespace FarmClaim.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IApplicationDbContext context,
            IJwtService jwtService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResponseDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Refreshing token from cookie...");

            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == cmd.RefreshToken
                                           && !rt.IsRevoked
                                           && rt.ExpiresAt > DateTime.UtcNow, ct);

            if (existingToken?.User == null)
            {
                _logger.LogWarning("Invalid refresh token: No valid token found");
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var user = existingToken.User;

            // === NEW: Check user status (a blocked/suspended user's token can't refresh) ===
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Non-active user {UserId} (Status={Status}) tried token refresh",
                    user.Id, user.Status);

                // Revoke the token they tried to use, just to be safe
                existingToken.IsRevoked = true;
                existingToken.RevokedAt = DateTime.UtcNow;
                existingToken.ReasonRevoked = $"User status is {user.Status}";
                await _context.SaveChangesAsync(ct);

                throw new ForbiddenException(
                    $"Your account is {user.Status.ToString().ToLower()}. " +
                    "Please contact support.");
            }

            // Step 2: REVOKE old token FIRST
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReasonRevoked = "Token rotation during refresh";

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Old token revoked for user: {UserId}", user.Id);

            // Step 3: Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshTokenValue = _jwtService.GenerateRefreshToken();

            // Step 4: Create NEW refresh token as SEPARATE entity
            var newRefreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = newRefreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(newRefreshTokenEntity, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenValue,
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
    }
}