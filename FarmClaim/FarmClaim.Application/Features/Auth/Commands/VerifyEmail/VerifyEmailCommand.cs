using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.VerifyEmail
{
    public record VerifyEmailCommand(
        VerifyEmailRequestDto Request,
        string? ClientIp = null
    ) : IRequest<VerifyEmailResponseDto>;
}