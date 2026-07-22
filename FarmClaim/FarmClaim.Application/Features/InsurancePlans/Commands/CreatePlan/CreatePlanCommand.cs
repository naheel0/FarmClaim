using FarmClaim.Application.Features.InsurancePlans.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.CreatePlan
{
    public record CreatePlanCommand(
        Guid AdminUserId,
        CreatePlanRequestDto Request
    ) : IRequest<PlanResponseDto>;
}