using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.ActivateUser
{
    public record ActivateUserCommand(
        Guid TargetUserId,
        Guid AdminUserId,
        UserActionRequestDto Request
    ) : IRequest<UserActionResponseDto>;
}