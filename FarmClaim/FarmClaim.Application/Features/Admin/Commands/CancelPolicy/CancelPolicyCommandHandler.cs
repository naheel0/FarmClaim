using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.CancelPolicy
{
    public class CancelPolicyCommandHandler : IRequestHandler<CancelPolicyCommand, ApprovePolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IAuditService _auditService;
        private readonly ILogger<CancelPolicyCommandHandler> _logger;

        public CancelPolicyCommandHandler(
            IApplicationDbContext context,
            IPaymentService paymentService,
            IAuditService auditService,
            ILogger<CancelPolicyCommandHandler> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApprovePolicyResponseDto> Handle(CancelPolicyCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} cancelling policy {PolicyId}", request.AdminUserId, request.PolicyId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(p => p.Id == request.PolicyId && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException($"Policy '{request.PolicyId}' not found.");

            if (policy.Status != PolicyStatus.Active)
                throw new InvalidOperationException(
                    $"Cannot cancel. Policy status is '{policy.Status}'. Only 'Active' policies can be cancelled.");

            // Check for active claims
            var hasActiveClaims = await _context.Claims
                .AnyAsync(c => c.PolicyId == request.PolicyId
                    && !c.IsDeleted
                    && c.Status != ClaimStatus.Pending
                    && c.Status != ClaimStatus.Rejected, ct);

            if (hasActiveClaims)
                throw new InvalidOperationException(
                    "Cannot cancel policy with active/approved/paid claims. " +
                    "Please resolve all claims before cancelling.");

            // Initiate refund if payment was captured
            var capturedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PolicyId == policy.Id
                    && p.Status == PaymentStatus.Captured
                    && !p.IsDeleted, ct);

            if (capturedPayment != null && !string.IsNullOrEmpty(capturedPayment.PaymentId))
            {
                _logger.LogWarning(
                    "Cancelling Active policy {PolicyId} with Captured payment {PaymentId}. Initiating refund.",
                    request.PolicyId, capturedPayment.Id);

                try
                {
                    var refundResult = await _paymentService.RefundPaymentAsync(
                        capturedPayment.PaymentId,
                        capturedPayment.AmountInRupees,
                        $"Policy cancelled by admin: {request.Reason}",
                        ct);

                    if (refundResult.Success)
                    {
                        capturedPayment.Status = PaymentStatus.Refunded;
                        capturedPayment.RefundedAt = DateTime.UtcNow;
                        capturedPayment.Notes = $"Refunded on cancellation. RefundId: {refundResult.RefundId}, Amount: ₹{refundResult.AmountRefunded}";
                        _logger.LogInformation("Refund initiated for Payment {PaymentId}: RefundId={RefundId}",
                            capturedPayment.Id, refundResult.RefundId);
                    }
                    else
                    {
                        _logger.LogError("Refund failed for Payment {PaymentId}: {Error}",
                            capturedPayment.Id, refundResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refund failed for Payment {PaymentId}. Admin must handle refund manually.", capturedPayment.Id);
                }
            }

            var oldStatus = policy.Status;

            policy.Status = PolicyStatus.Cancelled;
            policy.CancelledAt = DateTime.UtcNow;
            policy.RejectionReason = $"Cancelled by admin: {request.Reason}";
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogActionAsync(
                action: "policy.cancelled",
                entityType: "InsurancePolicy",
                entityId: request.PolicyId.ToString(),
                description: $"Policy cancelled. Status: {oldStatus} -> Cancelled. Reason: {request.Reason}",
                oldValue: new { status = oldStatus.ToString() },
                newValue: new { status = "Cancelled", reason = request.Reason },
                ct: ct);

            _logger.LogInformation("Policy {PolicyId} cancelled by Admin {AdminId}", request.PolicyId, request.AdminUserId);

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
