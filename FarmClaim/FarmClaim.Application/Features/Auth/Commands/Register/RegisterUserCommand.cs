using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.Register
{
    public record RegisterUserCommand(RegisterRequestDto Request) : IRequest<RegisterResponseDto>;
}