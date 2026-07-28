using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farms.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farms.Commands.UpdateFarm
{
    public class UpdateFarmCommandHandler : IRequestHandler<UpdateFarmCommand, FarmResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdateFarmCommandHandler> _logger;

        public UpdateFarmCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdateFarmCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FarmResponseDto> Handle(UpdateFarmCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Updating farm: {FarmId} for user: {UserId}", command.FarmId, command.UserId);

            var farm = await _context.Farms
                .Include(f => f.InsurancePolicies)
                .Include(f => f.Claims)
                .FirstOrDefaultAsync(f => f.Id == command.FarmId && f.UserId == command.UserId && !f.IsDeleted, ct);

            if (farm == null)
            {
                _logger.LogWarning("Farm not found: {FarmId} or not owned by user: {UserId}", command.FarmId, command.UserId);
                throw new NotFoundException(nameof(Farm), command.FarmId);
            }

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(command.Request.Name))
            {
                farm.Name = command.Request.Name.Trim();
                hasChanges = true;
            }

            if (command.Request.AreaInHectares.HasValue)
            {
                farm.AreaInHectares = command.Request.AreaInHectares.Value;
                hasChanges = true;
            }

            if (command.Request.Address != null)
            {
                farm.Address = command.Request.Address.Trim();
                hasChanges = true;
            }

            if (command.Request.IsActive.HasValue)
            {
                farm.IsActive = command.Request.IsActive.Value;
                hasChanges = true;
            }

            // FIXED: Now updates location fields
            if (command.Request.Latitude.HasValue)
            {
                farm.Latitude = command.Request.Latitude.Value;
                hasChanges = true;
            }

            if (command.Request.Longitude.HasValue)
            {
                farm.Longitude = command.Request.Longitude.Value;
                hasChanges = true;
            }

            if (command.Request.LocationGeoJson != null)
            {
                farm.LocationGeoJson = command.Request.LocationGeoJson;
                hasChanges = true;
            }

            if (hasChanges)
            {
                farm.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Farm updated: {FarmId}", farm.Id);
            }
            else
            {
                _logger.LogInformation("No changes detected for farm: {FarmId}", farm.Id);
            }

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
                PoliciesCount = farm.InsurancePolicies?.Count(p => !p.IsDeleted && p.Status == PolicyStatus.Active) ?? 0,
                ClaimsCount = farm.Claims?.Count(c => !c.IsDeleted) ?? 0
            };
        }
    }
}