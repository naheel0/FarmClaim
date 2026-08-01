using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Payments.Commands.VerifyPayment
{
    public class VerifyPaymentCommandHandler : IRequestHandler<VerifyPaymentCommand, VerifyPaymentResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IEmailQueueService _emailQueue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerifyPaymentCommandHandler> _logger;

        public VerifyPaymentCommandHandler(
            IApplicationDbContext context,
            IPaymentService paymentService,
            IEmailQueueService emailQueue,
            IConfiguration configuration,
            ILogger<VerifyPaymentCommandHandler> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _emailQueue = emailQueue;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<VerifyPaymentResponseDto> Handle(VerifyPaymentCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Verifying Razorpay payment: Order={OrderId}, Payment={PaymentId}",
                cmd.Request.RazorpayOrderId, cmd.Request.RazorpayPaymentId);

            var payment = await _context.Payments
                .Include(p => p.Policy).ThenInclude(p => p!.Farm).ThenInclude(f => f!.User)
                .Include(p => p.Policy).ThenInclude(p => p!.PremiumSchedules)
                .FirstOrDefaultAsync(p => p.OrderId == cmd.Request.RazorpayOrderId && !p.IsDeleted, ct);

            if (payment == null)
                throw new NotFoundException("Payment order not found for OrderId: " + cmd.Request.RazorpayOrderId);

            if (payment.UserId != cmd.UserId)
                throw new ForbiddenException("You can only verify your own payments.");

            if (payment.Status == PaymentStatus.Captured)
                throw new ValidationException(new List<string>
                {
                    $"Payment already captured on {payment.CapturedAt:yyyy-MM-dd HH:mm} UTC."
                });

            // Reject verification of expired orders (15-minute window)
            if (payment.CreatedAt.AddMinutes(15) < DateTime.UtcNow)
            {
                payment.Status = PaymentStatus.Expired;
                payment.FailureReason = "Payment order expired (15-minute window exceeded)";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                throw new ValidationException(new List<string>
                {
                    "Payment order has expired. Please create a new order."
                });
            }

            var isValid = await _paymentService.VerifySignatureAsync(
                cmd.Request.RazorpayOrderId,
                cmd.Request.RazorpayPaymentId,
                cmd.Request.RazorpaySignature);

            if (!isValid)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "Signature verification failed";
                payment.FailedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                _logger.LogWarning("Signature verification failed for Order {OrderId}", cmd.Request.RazorpayOrderId);
                throw new ValidationException(new List<string> { "Payment signature verification failed. Possible tampering detected." });
            }

            PaymentDetailsDto? details = null;
            try
            {
                details = await _paymentService.FetchPaymentDetailsAsync(cmd.Request.RazorpayPaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch payment details from Razorpay (non-fatal)");
            }

            payment.PaymentId = cmd.Request.RazorpayPaymentId;
            payment.Signature = cmd.Request.RazorpaySignature;
            payment.Status = PaymentStatus.Captured;
            payment.CapturedAt = DateTime.UtcNow;

            bool isInstallmentPayment = payment.PremiumScheduleId.HasValue;
            int installmentNumber = 0;
            int totalInstallments = 0;

            // Handle installment payments
            if (payment.PremiumScheduleId.HasValue)
            {
                var schedule = await _context.PremiumSchedules
                    .FirstOrDefaultAsync(s => s.Id == payment.PremiumScheduleId.Value
                        && !s.IsDeleted, ct);

                if (schedule == null || schedule.Status != PremiumScheduleStatus.Paid)
                {
                    if (schedule != null && schedule.Status == PremiumScheduleStatus.Pending)
                    {
                        schedule.Status = PremiumScheduleStatus.Paid;
                        schedule.PaidAt = DateTime.UtcNow;
                        schedule.PaymentId = payment.Id;
                    }

                    var schedules = await _context.PremiumSchedules
                        .Where(s => s.PolicyId == payment.PolicyId && !s.IsDeleted)
                        .ToListAsync(ct);

                    totalInstallments = schedules.Count;
                    installmentNumber = schedules.Count(s => s.Status == PremiumScheduleStatus.Paid);

                    if (payment.Policy != null)
                    {
                        payment.Policy.CurrentInstallmentNumber = installmentNumber;

                        var nextSchedule = schedules
                            .Where(s => s.Status == PremiumScheduleStatus.Pending)
                            .OrderBy(s => s.InstallmentNumber)
                            .FirstOrDefault();

                        payment.Policy.NextInstallmentDueDate = nextSchedule?.DueDate;

                        if (installmentNumber >= totalInstallments && totalInstallments > 0)
                        {
                            _logger.LogInformation(
                                "All installments paid for policy {PolicyId}. Transitioning to PaymentReceived.",
                                payment.Policy.Id);
                            payment.Policy.Status = PolicyStatus.PaymentReceived;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Schedule {ScheduleId} already paid — skipping installment update",
                        payment.PremiumScheduleId.Value);
                }
            }
            // Transition policy from Pending to PaymentReceived (single full payment)
            else if (payment.Policy != null && payment.Policy.Status == PolicyStatus.Pending)
            {
                payment.Policy.Status = PolicyStatus.PaymentReceived;
                _logger.LogInformation("Policy {PolicyId} transitioned to PaymentReceived after payment {PaymentId}", payment.PolicyId, payment.Id);
            }

            if (details != null)
            {
                payment.Method = details.Method;
                payment.BankReference = details.BankReference;
                payment.Fee = details.Fee;
                payment.Tax = details.Tax;

                payment.MethodDescription = !string.IsNullOrEmpty(details.CardLast4)
                    ? $"Card ****{details.CardLast4} ({details.CardNetwork})"
                    : !string.IsNullOrEmpty(details.Vpa)
                        ? $"UPI: {details.Vpa}"
                        : !string.IsNullOrEmpty(details.Bank)
                            ? $"NetBanking: {details.Bank}"
                            : !string.IsNullOrEmpty(details.Wallet)
                                ? $"Wallet: {details.Wallet}"
                                : details.Method;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Payment captured: PaymentId={PaymentId}, Amount=₹{Amount}, Method={Method}",
                payment.Id, payment.AmountInRupees, payment.Method);

            var farmer = payment.Policy?.Farm?.User;
            if (farmer != null)
            {
                var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";

                await _emailQueue.EnqueueEmailAsync(
                    toEmail: farmer.Email,
                    templateName: "PaymentSuccessEmail",
                    model: new PaymentSuccessEmailModel
                    {
                        FarmerName = $"{farmer.FirstName} {farmer.LastName}",
                        PolicyNumber = payment.Policy?.PolicyNumber ?? "",
                        Provider = payment.Policy?.Provider ?? "",
                        AmountPaid = payment.AmountInRupees,
                        PaymentMethod = payment.MethodDescription ?? payment.Method ?? "Online",
                        ReceiptNumber = payment.ReceiptNumber ?? "",
                        RazorpayPaymentId = cmd.Request.RazorpayPaymentId,
                        CapturedAt = payment.CapturedAt.Value,
                        DashboardUrl = $"{frontendBaseUrl}/policies/{payment.PolicyId}"
                    });
            }

            return new VerifyPaymentResponseDto
            {
                Success = true,
                Message = isInstallmentPayment
                    ? $"Installment {installmentNumber} of {totalInstallments} paid successfully. Policy remains pending until all installments are received."
                    : "Payment verified successfully. Your policy is now active.",
                PaymentId = payment.Id,
                PolicyId = payment.PolicyId,
                PolicyNumber = payment.Policy?.PolicyNumber,
                AmountPaid = payment.AmountInRupees,
                CapturedAt = payment.CapturedAt,
                ReceiptNumber = payment.ReceiptNumber
            };
        }
    }
}