using FarmClaim.Application.Features.Payments.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Payments.Commands.CreateOrder
{
    public record CreateOrderCommand(
        Guid PolicyId,
        Guid UserId,
        CreateOrderRequestDto Request,
        string? ClientIp = null,
        string? UserAgent = null
    ) : IRequest<CreateOrderResponseDto>;
}