using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.RenewPolicy
{
    public record RenewPolicyCommand(
        Guid PolicyId,
        Guid UserId,
        DateTime? StartDate
    ) : IRequest<PolicyResponseDto>;
}
