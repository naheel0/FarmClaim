using MediatR;
using FarmClaim.Application.Features.Auth.DTOs;

namespace FarmClaim.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(LoginRequestDto Request) : IRequest<AuthResponseDto>;
}