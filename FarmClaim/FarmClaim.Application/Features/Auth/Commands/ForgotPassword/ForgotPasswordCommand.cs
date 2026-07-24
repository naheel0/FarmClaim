using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(
        ForgotPasswordRequestDto Request,
        string? ClientIp = null
    ) : IRequest<PasswordResetResponseDto>;
}