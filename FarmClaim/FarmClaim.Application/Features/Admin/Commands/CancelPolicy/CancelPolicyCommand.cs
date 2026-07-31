using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.CancelPolicy
{
    public record CancelPolicyCommand(
        Guid PolicyId,
        Guid AdminUserId,
        string Reason
    ) : IRequest<ApprovePolicyResponseDto>;
}
