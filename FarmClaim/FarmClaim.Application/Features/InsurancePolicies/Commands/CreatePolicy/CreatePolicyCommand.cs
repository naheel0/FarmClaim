using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.CreatePolicy
{
    public record CreatePolicyCommand(
        Guid UserId,
        CreatePolicyRequestDto Request
    ) : IRequest<PolicyResponseDto>;
}