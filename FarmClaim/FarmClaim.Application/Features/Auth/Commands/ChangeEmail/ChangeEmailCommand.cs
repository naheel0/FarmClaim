using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.ChangeEmail
{
    public record ChangeEmailCommand(
        Guid UserId,
        ChangeEmailRequestDto Request,
        string? ClientIp = null
    ) : IRequest<EmailChangeResponseDto>;
}