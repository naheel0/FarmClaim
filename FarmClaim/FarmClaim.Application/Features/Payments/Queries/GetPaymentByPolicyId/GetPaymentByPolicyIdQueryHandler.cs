using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Payments.Queries.GetPaymentByPolicyId
{
    public class GetPaymentByPolicyIdQueryHandler : IRequestHandler<GetPaymentByPolicyIdQuery, List<PaymentResponseDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPaymentByPolicyIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaymentResponseDto>> Handle(GetPaymentByPolicyIdQuery request, CancellationToken ct)
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Policy)
                .Where(p => p.PolicyId == request.PolicyId
                            && p.UserId == request.UserId
                            && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return payments.Select(payment => new PaymentResponseDto
            {
                Id = payment.Id,
                PolicyId = payment.PolicyId,
                PolicyNumber = payment.Policy?.PolicyNumber ?? "",
                UserId = payment.UserId,
                OrderId = payment.OrderId,
                PaymentId = payment.PaymentId,
                AmountInRupees = payment.AmountInRupees,
                Currency = payment.Currency,
                Status = payment.Status,
                Method = payment.Method,
                MethodDescription = payment.MethodDescription,
                BankReference = payment.BankReference,
                FailureReason = payment.FailureReason,
                Fee = payment.Fee,
                Tax = payment.Tax,
                CapturedAt = payment.CapturedAt,
                FailedAt = payment.FailedAt,
                ReceiptNumber = payment.ReceiptNumber,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            }).ToList();
        }
    }
}