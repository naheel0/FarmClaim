using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Queries.GetPolicyById
{
    public record GetPolicyByIdQuery(
        Guid PolicyId,
        Guid UserId,
        UserRole? Role = null
    ) : IRequest<PolicyResponseDto>;
}