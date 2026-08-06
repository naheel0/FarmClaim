using FarmClaim.Application.Features.Payments.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Payments.Queries.GetPaymentByPolicyId
{
    public record GetPaymentByPolicyIdQuery(Guid PolicyId, Guid UserId) : IRequest<List<PaymentResponseDto>>;
}