using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.ApprovePolicy
{
    public record ApprovePolicyCommand(
        Guid PolicyId,
        Guid AdminUserId
    ) : IRequest<ApprovePolicyResponseDto>;
}