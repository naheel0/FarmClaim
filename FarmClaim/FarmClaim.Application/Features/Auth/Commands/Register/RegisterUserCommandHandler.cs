using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Auth.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = FarmClaim.Domain.Entities.RefreshToken;

namespace FarmClaim.Application.Features.Auth.Commands.Register
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IApplicationDbContext context,
            IJwtService jwtService,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Registering user: {Email}", request.Request.Email);

            var exists = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Request.Email.ToLower(), ct);

            if (exists != null)
                throw new InvalidOperationException($"Email '{request.Request.Email}' already registered.");

            var hasher = new PasswordHasher<User>();

            var user = new User
            {
                Email = request.Request.Email.ToLower().Trim(),
                FirstName = request.Request.FirstName.Trim(),
                LastName = request.Request.LastName.Trim(),
                PhoneNumber = request.Request.PhoneNumber?.Trim(),
                Role = request.Request.Role
            };

            user.PasswordHash = hasher.HashPassword(user, request.Request.Password);

            await _context.Users.AddAsync(user, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User registered: {UserId}", user.Id);

            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity, ct);
            await _context.SaveChangesAsync(ct);

            return new AuthResponseDto
            {
                AccessToken = _jwtService.GenerateAccessToken(user),
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