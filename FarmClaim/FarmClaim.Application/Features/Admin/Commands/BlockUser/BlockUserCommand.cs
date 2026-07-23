using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.BlockUser
{
    public record BlockUserCommand(
        Guid TargetUserId,
        Guid AdminUserId,
        UserActionRequestDto Request
    ) : IRequest<UserActionResponseDto>;
}