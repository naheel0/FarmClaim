using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Queries.GetPolicyById
{
    public record GetPolicyByIdQuery(
        Guid PolicyId,
        Guid UserId
    ) : IRequest<PolicyResponseDto>;
}