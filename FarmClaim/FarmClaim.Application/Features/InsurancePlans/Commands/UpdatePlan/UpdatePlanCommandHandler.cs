using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.UpdatePlan
{
    public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, PlanResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdatePlanCommandHandler> _logger;

        public UpdatePlanCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdatePlanCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PlanResponseDto> Handle(UpdatePlanCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} updating plan {PlanId}",
                command.AdminUserId, command.PlanId);

            var plan = await _context.InsurancePlans
                .Include(p => p.Policies)
                .FirstOrDefaultAsync(p => p.Id == command.PlanId && !p.IsDeleted, ct);

            if (plan == null)
                throw new NotFoundException(nameof(InsurancePlan), command.PlanId);

            // Name uniqueness check (excluding self)
            var nameConflict = await _context.InsurancePlans
                .AnyAsync(p => p.Id != plan.Id
                               && p.Name.ToLower() == command.Request.Name.Trim().ToLower()
                               && !p.IsDeleted, ct);
            if (nameConflict)
                throw new ValidationException(new List<string>
                {
                    $"Another plan with name '{command.Request.Name}' already exists"
                });

            plan.Name = command.Request.Name.Trim();
            plan.Description = command.Request.Description?.Trim();
            plan.CropType = command.Request.CropType.Trim();
            plan.Provider = command.Request.Provider.Trim();
            plan.PremiumRatePerHectare = command.Request.PremiumRatePerHectare;
            plan.SumInsuredPerHectare = command.Request.SumInsuredPerHectare;
            plan.CoveragePercentage = command.Request.CoveragePercentage;
            plan.MinAreaInHectares = command.Request.MinAreaInHectares;
            plan.MaxAreaInHectares = command.Request.MaxAreaInHectares;
            plan.PolicyDurationMonths = command.Request.PolicyDurationMonths;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Plan {PlanId} updated by Admin {AdminId}",
                plan.Id, command.AdminUserId);

            return new PlanResponseDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                CropType = plan.CropType,
                Provider = plan.Provider,
                PremiumRatePerHectare = plan.PremiumRatePerHectare,
                SumInsuredPerHectare = plan.SumInsuredPerHectare,
                CoveragePercentage = plan.CoveragePercentage,
                MinAreaInHectares = plan.MinAreaInHectares,
                MaxAreaInHectares = plan.MaxAreaInHectares,
                PolicyDurationMonths = plan.PolicyDurationMonths,
                IsActive = plan.IsActive,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                PoliciesCount = plan.Policies.Count(p => !p.IsDeleted)
            };
        }
    }
}