using FarmClaim.Application.Features.InsurancePlans.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.TogglePlanStatus
{
    public record TogglePlanStatusCommand(
        Guid PlanId,
        Guid AdminUserId,
        bool Activate
    ) : IRequest<PlanResponseDto>;
}