using MediatR;
using FarmClaim.Application.Features.Auth.DTOs;

namespace FarmClaim.Application.Features.Auth.Commands.Register
{
    public record RegisterUserCommand(RegisterRequestDto Request) : IRequest<AuthResponseDto>;
}