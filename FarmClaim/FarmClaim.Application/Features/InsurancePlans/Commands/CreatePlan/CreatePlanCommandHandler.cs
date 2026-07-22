using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.CreatePlan
{
    public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, PlanResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CreatePlanCommandHandler> _logger;

        public CreatePlanCommandHandler(
            IApplicationDbContext context,
            ILogger<CreatePlanCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PlanResponseDto> Handle(CreatePlanCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} creating insurance plan '{Name}'",
                command.AdminUserId, command.Request.Name);

            var nameExists = await _context.InsurancePlans
                .AnyAsync(p => p.Name.ToLower() == command.Request.Name.Trim().ToLower()
                               && !p.IsDeleted, ct);

            if (nameExists)
                throw new ValidationException(new List<string>
                {
                    $"A plan with name '{command.Request.Name}' already exists"
                });

            var plan = new InsurancePlan
            {
                Name = command.Request.Name.Trim(),
                Description = command.Request.Description?.Trim(),
                CropType = command.Request.CropType.Trim(),
                Provider = command.Request.Provider.Trim(),
                PremiumRatePerHectare = command.Request.PremiumRatePerHectare,
                SumInsuredPerHectare = command.Request.SumInsuredPerHectare,
                CoveragePercentage = command.Request.CoveragePercentage,
                MinAreaInHectares = command.Request.MinAreaInHectares,
                MaxAreaInHectares = command.Request.MaxAreaInHectares,
                PolicyDurationMonths = command.Request.PolicyDurationMonths,
                IsActive = command.Request.IsActive
            };

            await _context.InsurancePlans.AddAsync(plan, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Insurance plan created: {PlanId}, Name: {Name}",
                plan.Id, plan.Name);

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
                PoliciesCount = 0
            };
        }
    }
}