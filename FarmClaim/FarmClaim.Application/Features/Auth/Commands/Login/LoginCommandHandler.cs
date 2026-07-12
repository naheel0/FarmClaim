using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
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

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User logged in: {UserId}", user.Id);

            return await GenerateAuthResponseAsync(user, ct);
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user, CancellationToken ct)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            if (user.RefreshToken != null)
            {
                user.RefreshToken.IsRevoked = true;
                user.RefreshToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            var newRefreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = refreshTokenValue,
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
    }
}