using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.DeletePlan
{
    public record DeletePlanCommand(
        Guid PlanId,
        Guid AdminUserId
    ) : IRequest<Unit>;
}