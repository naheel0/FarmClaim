using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.TogglePlanStatus
{
	public class TogglePlanStatusCommandHandler : IRequestHandler<TogglePlanStatusCommand, PlanResponseDto>
	{
		private readonly IApplicationDbContext _context;
		private readonly ILogger<TogglePlanStatusCommandHandler> _logger;

		public TogglePlanStatusCommandHandler(
			IApplicationDbContext context,
			ILogger<TogglePlanStatusCommandHandler> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<PlanResponseDto> Handle(TogglePlanStatusCommand command, CancellationToken ct)
		{
			var plan = await _context.InsurancePlans
				.Include(p => p.Policies)
				.FirstOrDefaultAsync(p => p.Id == command.PlanId && !p.IsDeleted, ct);

			if (plan == null)
				throw new NotFoundException(nameof(InsurancePlan), command.PlanId);

			if (command.Activate && plan.IsActive)
				throw new ValidationException(new List<string> { "Plan is already active." });

			if (!command.Activate && !plan.IsActive)
				throw new ValidationException(new List<string> { "Plan is already inactive." });

			plan.IsActive = command.Activate;
			await _context.SaveChangesAsync(ct);

			_logger.LogInformation("Plan {PlanId} {Action} by Admin {AdminId}",
				plan.Id, command.Activate ? "activated" : "deactivated", command.AdminUserId);

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