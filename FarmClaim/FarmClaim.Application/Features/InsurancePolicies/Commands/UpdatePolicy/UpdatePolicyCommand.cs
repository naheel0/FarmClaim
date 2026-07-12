using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.UpdatePolicy
{
    public record UpdatePolicyCommand(
        Guid PolicyId,
        Guid UserId,
        UpdatePolicyRequestDto Request
    ) : IRequest<PolicyResponseDto>;
}