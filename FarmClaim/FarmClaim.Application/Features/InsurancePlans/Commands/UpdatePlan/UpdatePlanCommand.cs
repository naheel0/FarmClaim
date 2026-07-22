using FarmClaim.Application.Features.InsurancePlans.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.UpdatePlan
{
    public record UpdatePlanCommand(
        Guid PlanId,
        Guid AdminUserId,
        UpdatePlanRequestDto Request
    ) : IRequest<PlanResponseDto>;
}