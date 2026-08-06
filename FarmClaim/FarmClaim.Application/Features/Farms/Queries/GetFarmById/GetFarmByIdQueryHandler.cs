using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farms.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farms.Queries.GetFarmById
{
    public class GetFarmByIdQueryHandler : IRequestHandler<GetFarmByIdQuery, FarmResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetFarmByIdQueryHandler> _logger;

        public GetFarmByIdQueryHandler(
            IApplicationDbContext context,
            ILogger<GetFarmByIdQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FarmResponseDto> Handle(GetFarmByIdQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting farm {FarmId} for user {UserId}", request.FarmId, request.UserId);

            // FIXED: Now checks user ownership
            var farm = await _context.Farms
                .AsNoTracking()
                .Include(f => f.InsurancePolicies)
                .Include(f => f.Claims)
                .FirstOrDefaultAsync(f => f.Id == request.FarmId
                    && f.UserId == request.UserId // FIXED: Ownership check added
                    && !f.IsDeleted, ct);

            if (farm == null)
            {
                _logger.LogWarning("Farm not found: {FarmId} or not owned by user: {UserId}", request.FarmId, request.UserId);
                throw new NotFoundException(nameof(Farm), request.FarmId);
            }

            _logger.LogInformation("Farm {FarmId} retrieved successfully", farm.Id);

            return new FarmResponseDto
            {
                Id = farm.Id,
                UserId = farm.UserId,
                Name = farm.Name,
                AreaInHectares = farm.AreaInHectares,
                Address = farm.Address,
                Latitude = farm.Latitude,
                Longitude = farm.Longitude,
                LocationGeoJson = farm.LocationGeoJson,
                CreatedAt = farm.CreatedAt,
                UpdatedAt = farm.UpdatedAt,
                IsActive = farm.IsActive,
                PoliciesCount = farm.InsurancePolicies.Count(p => !p.IsDeleted),
                ClaimsCount = farm.Claims.Count(c => !c.IsDeleted)
            };
        }
    }
}