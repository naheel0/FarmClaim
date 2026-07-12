using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farmers.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile
{
    public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, FarmerProfileDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetCurrentUserProfileQueryHandler> _logger;

        public GetCurrentUserProfileQueryHandler(
            IApplicationDbContext context,
            ILogger<GetCurrentUserProfileQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FarmerProfileDto> Handle(GetCurrentUserProfileQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting profile for user: {UserId}", request.UserId);

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Farms)
                .Include(u => u.Policies)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, ct);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                throw new NotFoundException(nameof(User), request.UserId);
            }

            _logger.LogInformation("Profile retrieved successfully for: {Email}", user.Email);

            return new FarmerProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalFarms = user.Farms?.Count(f => !f.IsDeleted) ?? 0,
                TotalPolicies = user.Policies?.Count(p => !p.IsDeleted && p.IsActive) ?? 0,
                TotalClaims = user.Claims?.Count(c => !c.IsDeleted) ?? 0
            };
        }
    }
}