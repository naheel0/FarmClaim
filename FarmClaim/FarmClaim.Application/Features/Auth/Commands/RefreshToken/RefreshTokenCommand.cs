using MediatR;
using FarmClaim.Application.Features.Auth.DTOs;

namespace FarmClaim.Application.Features.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponseDto>;
}