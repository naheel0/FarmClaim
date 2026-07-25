using FarmClaim.Application.Features.Payments.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Payments.Commands.VerifyPayment
{
    public record VerifyPaymentCommand(
        VerifyPaymentRequestDto Request,
        Guid UserId,
        string? ClientIp = null
    ) : IRequest<VerifyPaymentResponseDto>;
}