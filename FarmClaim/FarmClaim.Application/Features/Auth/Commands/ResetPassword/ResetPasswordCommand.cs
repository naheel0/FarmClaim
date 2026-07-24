using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(
        ResetPasswordRequestDto Request,
        string? ClientIp = null
    ) : IRequest<PasswordResetResponseDto>;
}