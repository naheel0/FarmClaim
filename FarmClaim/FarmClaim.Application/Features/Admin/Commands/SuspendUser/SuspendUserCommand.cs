using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.SuspendUser
{
    public record SuspendUserCommand(
        Guid TargetUserId,
        Guid AdminUserId,
        UserActionRequestDto Request
    ) : IRequest<UserActionResponseDto>;
}