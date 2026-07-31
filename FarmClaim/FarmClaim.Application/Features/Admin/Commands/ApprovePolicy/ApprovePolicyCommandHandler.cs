using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.ApprovePolicy
{
    public class ApprovePolicyCommandHandler : IRequestHandler<ApprovePolicyCommand, ApprovePolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ApprovePolicyCommandHandler> _logger;

        public ApprovePolicyCommandHandler(
            IApplicationDbContext context,
            IAuditService auditService,
            ILogger<ApprovePolicyCommandHandler> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApprovePolicyResponseDto> Handle(ApprovePolicyCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} approving policy {PolicyId}", request.AdminUserId, request.PolicyId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(p => p.Id == request.PolicyId && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException($"Policy '{request.PolicyId}' not found.");

            if (policy.Status != PolicyStatus.PaymentReceived)
                throw new InvalidOperationException(
                    $"Cannot approve. Policy status is '{policy.Status}'. Only policies with confirmed payment (PaymentReceived) can be approved.");

            // Verify that a Captured payment exists
            var hasCapturedPayment = await _context.Payments
                .AnyAsync(p => p.PolicyId == policy.Id
                    && p.Status == PaymentStatus.Captured
                    && !p.IsDeleted, ct);

            if (!hasCapturedPayment)
                throw new InvalidOperationException(
                    "Cannot approve policy without a confirmed payment. " +
                    "The payment may have been reversed. Please check payment status.");

            var oldStatus = policy.Status;

            policy.Status = PolicyStatus.Active;
            policy.ApprovedAt = DateTime.UtcNow;
            policy.ApprovedByUserId = request.AdminUserId;
            policy.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict approving policy {PolicyId}", request.PolicyId);
                throw new InvalidOperationException(
                    "This policy was modified by another operation. Please refresh and try again.");
            }

            await _auditService.LogActionAsync(
                action: "policy.approved",
                entityType: "InsurancePolicy",
                entityId: request.PolicyId.ToString(),
                description: $"Policy approved. Status: {oldStatus} -> Active",
                oldValue: new { status = oldStatus.ToString() },
                newValue: new { status = "Active", approvedBy = request.AdminUserId },
                ct: ct);

            _logger.LogInformation("Policy {PolicyId} approved by Admin {AdminId}", request.PolicyId, request.AdminUserId);

            return new ApprovePolicyResponseDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                Status = policy.Status,
                ApprovedAt = policy.ApprovedAt,
                ApprovedByUserId = policy.ApprovedByUserId,
                ApprovedByName = policy.ApprovedByUser != null
                    ? $"{policy.ApprovedByUser.FirstName} {policy.ApprovedByUser.LastName}"
                    : null
            };
        }
    }
}
