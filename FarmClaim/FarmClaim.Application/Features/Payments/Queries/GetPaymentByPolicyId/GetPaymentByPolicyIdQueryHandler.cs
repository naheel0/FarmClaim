using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Payments.Queries.GetPaymentByPolicyId
{
    public class GetPaymentByPolicyIdQueryHandler : IRequestHandler<GetPaymentByPolicyIdQuery, PaymentResponseDto>
    {
        private readonly IApplicationDbContext _context;

        public GetPaymentByPolicyIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentResponseDto> Handle(GetPaymentByPolicyIdQuery request, CancellationToken ct)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Policy)
                .Where(p => p.PolicyId == request.PolicyId
                            && p.UserId == request.UserId
                            && p.Status == PaymentStatus.Captured
                            && !p.IsDeleted)
                .OrderByDescending(p => p.CapturedAt)
                .FirstOrDefaultAsync(ct);

            if (payment == null)
                throw new NotFoundException("No successful payment found for this policy.");

            return new PaymentResponseDto
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
            };
        }
    }
}