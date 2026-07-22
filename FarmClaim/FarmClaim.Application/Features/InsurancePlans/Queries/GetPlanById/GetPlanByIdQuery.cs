using FarmClaim.Application.Features.InsurancePlans.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Queries.GetPlanById
{
    public record GetPlanByIdQuery(
        Guid PlanId,
        bool AdminContext = false
    ) : IRequest<PlanResponseDto>;
}