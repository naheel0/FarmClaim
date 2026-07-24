using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.ConfirmEmailChange
{
    public record ConfirmEmailChangeCommand(
        ConfirmEmailChangeDto Request,
        string? ClientIp = null
    ) : IRequest<EmailChangeResponseDto>;
}