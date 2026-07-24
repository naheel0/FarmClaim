using FarmClaim.Application.Features.Auth.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.ResendOtp
{
    public record ResendOtpCommand(ResendOtpRequestDto Request, string? ClientIp = null)
        : IRequest<VerifyEmailResponseDto>;
}