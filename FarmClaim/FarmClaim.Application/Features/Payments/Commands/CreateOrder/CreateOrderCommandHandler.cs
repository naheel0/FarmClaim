using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Payments.Commands.CreateOrder
{
    public class CreateOrderCommandHandler
        : IRequestHandler<CreateOrderCommand, CreateOrderResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IApplicationDbContext context,
            IPaymentService paymentService,
            ILogger<CreateOrderCommandHandler> logger
        )
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<CreateOrderResponseDto> Handle(
            CreateOrderCommand cmd,
            CancellationToken ct
        )
        {
            _logger.LogInformation(
                "Creating Razorpay order for policy {PolicyId} by user {UserId}",
                cmd.PolicyId,
                cmd.UserId
            );

            var policy = await _context
                .InsurancePolicies.Include(p => p.Farm)
                    .ThenInclude(f => f!.User)
                .FirstOrDefaultAsync(p => p.Id == cmd.PolicyId && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException(nameof(InsurancePolicy), cmd.PolicyId);

            if (policy.Farm?.UserId != cmd.UserId)
                throw new ForbiddenException("You can only pay for your own policies.");

            // Allow payment only for Pending policies (PaymentReceived means already paid, Active means approved)
            if (policy.Status != PolicyStatus.Pending)
                throw new ValidationException(
                    new List<string>
                    {
                        $"Policy must be Pending to collect payment. Current status: {policy.Status}.",
                    }
                );

            var existingPayment = await _context
                .Payments.Where(p =>
                    p.PolicyId == policy.Id && p.Status == PaymentStatus.Captured && !p.IsDeleted
                )
                .FirstOrDefaultAsync(ct);

            if (existingPayment != null && cmd.Request.PremiumScheduleId == null)
                throw new ValidationException(
                    new List<string>
                    {
                        $"Premium already paid on {existingPayment.CapturedAt:yyyy-MM-dd}. Receipt: {existingPayment.ReceiptNumber}",
                    }
                );

            decimal amount;
            Guid? premiumScheduleId = null;

            if (cmd.Request.PremiumScheduleId.HasValue)
            {
                var schedule = await _context.PremiumSchedules
                    .FirstOrDefaultAsync(s =>
                        s.Id == cmd.Request.PremiumScheduleId.Value
                        && s.PolicyId == policy.Id
                        && s.Status == PremiumScheduleStatus.Pending
                        && !s.IsDeleted, ct);

                if (schedule == null)
                    throw new NotFoundException(nameof(PremiumSchedule), cmd.Request.PremiumScheduleId.Value);

                amount = schedule.AmountDue;
                premiumScheduleId = schedule.Id;

                if (amount <= 0)
                    throw new ValidationException(
                        new List<string> { "Installment amount must be greater than 0." }
                    );
            }
            else
            {
                // Full premium payment — legacy path
                amount = policy.Premium;
                if (amount <= 0)
                    throw new ValidationException(
                        new List<string> { "Policy premium must be greater than 0." }
                    );
            }

            // FIXED: Use .ToString() instead of :ToString
            var receiptNumber =
                $"RCT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

            var order = await _paymentService.CreateOrderAsync(
                amountInRupees: amount,
                currency: "INR",
                receipt: receiptNumber,
                policyId: policy.Id,
                userId: cmd.UserId,
                ct: ct
            );

            var payment = new Payment
            {
                PolicyId = policy.Id,
                UserId = cmd.UserId,
                OrderId = order.OrderId,
                AmountInPaise = order.AmountInPaise,
                AmountInRupees = amount,
                Currency = "INR",
                Status = PaymentStatus.Created,
                ReceiptNumber = receiptNumber,
                ClientIp = cmd.ClientIp,
                UserAgent = cmd.UserAgent,
                PremiumScheduleId = premiumScheduleId,
            };

            await _context.Payments.AddAsync(payment, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Razorpay order created: OrderId={OrderId}, PaymentId={PaymentId}, Amount={Amount}",
                order.OrderId,
                payment.Id,
                amount
            );

            return new CreateOrderResponseDto
            {
                PaymentId = payment.Id,
                PolicyId = policy.Id,
                OrderId = order.OrderId,
                AmountInPaise = order.AmountInPaise,
                AmountInRupees = amount,
                Currency = "INR",
                RazorpayKeyId = order.RazorpayKeyId,
                ReceiptNumber = receiptNumber,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Status = "Created",
                Customer = new CustomerInfo
                {
                    Name = $"{policy.Farm?.User?.FirstName} {policy.Farm?.User?.LastName}",
                    Email = policy.Farm?.User?.Email ?? "",
                    Phone = policy.Farm?.User?.PhoneNumber,
                },
            };
        }
    }
}
