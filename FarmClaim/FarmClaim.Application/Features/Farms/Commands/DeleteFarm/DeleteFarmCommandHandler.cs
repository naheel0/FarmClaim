using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farms.Commands.DeleteFarm
{
    public class DeleteFarmCommandHandler : IRequestHandler<DeleteFarmCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DeleteFarmCommandHandler> _logger;

        public DeleteFarmCommandHandler(
            IApplicationDbContext context,
            ILogger<DeleteFarmCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(DeleteFarmCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Deleting farm: {FarmId} for user: {UserId}", command.FarmId, command.UserId);

            var farm = await _context.Farms
                .FirstOrDefaultAsync(f => f.Id == command.FarmId && f.UserId == command.UserId && !f.IsDeleted, ct);

            if (farm == null)
            {
                _logger.LogWarning("Farm not found: {FarmId} or not owned by user: {UserId}", command.FarmId, command.UserId);
                throw new NotFoundException(nameof(Farm), command.FarmId);
            }

            // Block deletion if farm has active or pending policies
            var hasActivePolicies = await _context.InsurancePolicies
                .AnyAsync(p => p.FarmId == farm.Id
                    && !p.IsDeleted
                    && (p.Status == PolicyStatus.Active || p.Status == PolicyStatus.Pending), ct);

            if (hasActivePolicies)
            {
                throw new ValidationException(new List<string>
                {
                    "Cannot delete a farm with active or pending insurance policies. Cancel or let policies expire first."
                });
            }

            // Soft delete
            farm.IsDeleted = true;
            farm.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Farm deleted successfully: {FarmId}", command.FarmId);

            return true;
        }
    }
}