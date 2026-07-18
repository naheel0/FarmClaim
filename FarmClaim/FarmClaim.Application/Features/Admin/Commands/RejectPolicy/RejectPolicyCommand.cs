using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.RejectPolicy
{
    public record RejectPolicyCommand(
        Guid PolicyId,
        Guid AdminUserId,
        RejectPolicyRequestDto Request
    ) : IRequest<ApprovePolicyResponseDto>;
}