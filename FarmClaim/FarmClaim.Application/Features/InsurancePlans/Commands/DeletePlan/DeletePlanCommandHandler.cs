using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.DeletePlan
{
    public class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DeletePlanCommandHandler> _logger;

        public DeletePlanCommandHandler(
            IApplicationDbContext context,
            ILogger<DeletePlanCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeletePlanCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} deleting plan {PlanId}",
                command.AdminUserId, command.PlanId);

            var plan = await _context.InsurancePlans
                .Include(p => p.Policies)
                .FirstOrDefaultAsync(p => p.Id == command.PlanId && !p.IsDeleted, ct);

            if (plan == null)
                throw new NotFoundException(nameof(InsurancePlan), command.PlanId);

            var hasActivePolicies = plan.Policies.Any(p =>
                !p.IsDeleted
                && (p.Status == PolicyStatus.Pending || p.Status == PolicyStatus.Active));

            if (hasActivePolicies)
                throw new ValidationException(new List<string>
                {
                    "Cannot delete plan: there are active or pending policies linked to it. " +
                    "Deactivate the plan instead."
                });

            plan.IsDeleted = true;
            plan.IsActive = false;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Plan {PlanId} soft-deleted by Admin {AdminId}",
                plan.Id, command.AdminUserId);

            return Unit.Value;
        }
    }
}