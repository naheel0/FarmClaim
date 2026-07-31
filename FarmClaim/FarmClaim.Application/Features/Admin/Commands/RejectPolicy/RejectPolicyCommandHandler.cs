using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.RejectPolicy
{
    public class RejectPolicyCommandHandler : IRequestHandler<RejectPolicyCommand, ApprovePolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RejectPolicyCommandHandler> _logger;

        public RejectPolicyCommandHandler(
            IApplicationDbContext context,
            IPaymentService paymentService,
            IAuditService auditService,
            ILogger<RejectPolicyCommandHandler> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApprovePolicyResponseDto> Handle(RejectPolicyCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} rejecting policy {PolicyId}", request.AdminUserId, request.PolicyId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(p => p.Id == request.PolicyId && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException($"Policy '{request.PolicyId}' not found.");

            if (policy.Status != PolicyStatus.Pending && policy.Status != PolicyStatus.PaymentReceived)
                throw new InvalidOperationException(
                    $"Cannot reject. Policy status is '{policy.Status}'. Only 'Pending' or 'PaymentReceived' policies can be rejected.");

            // SAFETY NET: Check for Captured payment — initiate refund if exists
            var capturedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PolicyId == policy.Id
                    && p.Status == PaymentStatus.Captured
                    && !p.IsDeleted, ct);

            if (capturedPayment != null)
            {
                _logger.LogWarning(
                    "Admin {AdminId} rejecting policy {PolicyId} which has a Captured payment {PaymentId}. Initiating refund.",
                    request.AdminUserId, request.PolicyId, capturedPayment.Id);

                if (!string.IsNullOrEmpty(capturedPayment.PaymentId))
                {
                    try
                    {
                        var refundResult = await _paymentService.RefundPaymentAsync(
                            capturedPayment.PaymentId,
                            capturedPayment.AmountInRupees,
                            $"Policy rejected by admin: {request.Request.Reason}",
                            ct);

                        if (refundResult.Success)
                        {
                            capturedPayment.Status = PaymentStatus.Refunded;
                            capturedPayment.RefundedAt = DateTime.UtcNow;
                            capturedPayment.Notes = $"Refunded on rejection. RefundId: {refundResult.RefundId}, Amount: ₹{refundResult.AmountRefunded}";
                            _logger.LogInformation("Refund initiated for Payment {PaymentId}: RefundId={RefundId}",
                                capturedPayment.Id, refundResult.RefundId);
                        }
                        else
                        {
                            _logger.LogError("Refund failed for Payment {PaymentId}: {Error}",
                                capturedPayment.Id, refundResult.ErrorMessage);
                            // Policy still gets rejected — admin must handle refund manually via Razorpay dashboard
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Refund failed for Payment {PaymentId}. Admin must handle refund manually.", capturedPayment.Id);
                    }
                }
            }

            var oldStatus = policy.Status;

            policy.Status = PolicyStatus.Rejected;
            policy.RejectedAt = DateTime.UtcNow;
            policy.RejectionReason = request.Request.Reason.Trim();
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogActionAsync(
                action: "policy.rejected",
                entityType: "InsurancePolicy",
                entityId: request.PolicyId.ToString(),
                description: $"Policy rejected. Status: {oldStatus} -> Rejected. Reason: {request.Request.Reason}",
                oldValue: new { status = oldStatus.ToString() },
                newValue: new { status = "Rejected", reason = request.Request.Reason },
                ct: ct);

            _logger.LogInformation("Policy {PolicyId} rejected. Reason: {Reason}", request.PolicyId, request.Request.Reason);

            return new ApprovePolicyResponseDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                Status = policy.Status,
                ApprovedAt = null,
                ApprovedByUserId = null,
                ApprovedByName = null
            };
        }
    }
}
