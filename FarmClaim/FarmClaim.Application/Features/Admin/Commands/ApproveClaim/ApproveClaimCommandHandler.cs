using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.ApproveClaim
{
    public class ApproveClaimCommandHandler : IRequestHandler<ApproveClaimCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ApproveClaimCommandHandler> _logger;

        public ApproveClaimCommandHandler(
            IApplicationDbContext context,
            IAuditService auditService,
            ILogger<ApproveClaimCommandHandler> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(ApproveClaimCommand request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException("Claim not found");

            if (claim.Status == ClaimStatus.Approved)
                throw new ValidationException(new List<string> { "Claim is already approved" });

            if (claim.Status == ClaimStatus.Rejected)
                throw new ValidationException(new List<string> { "Cannot approve a rejected claim" });

            if (claim.Status == ClaimStatus.Paid)
                throw new ValidationException(new List<string> { "Claim is already paid" });

            if (request.Request.ApprovedAmount > (claim.Policy?.SumInsured ?? 0))
                throw new ValidationException(new List<string>
                {
                    $"Approved amount cannot exceed policy sum insured ({claim.Policy?.SumInsured})"
                });

            // Cumulative check: total approved claims must not exceed SumInsured
            var totalApproved = await _context.Claims
                .Where(c => c.PolicyId == claim.PolicyId
                    && !c.IsDeleted
                    && c.Id != claim.Id
                    && (c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Paid))
                .SumAsync(c => c.ApprovedAmount ?? 0, ct);

            if (totalApproved + request.Request.ApprovedAmount > (claim.Policy?.SumInsured ?? 0))
                throw new ValidationException(new List<string>
                {
                    $"Total approved claims (₹{totalApproved + request.Request.ApprovedAmount:N2}) would exceed policy sum insured (₹{claim.Policy?.SumInsured:N2}). " +
                    $"Already approved: ₹{totalApproved:N2}"
                });

            var oldStatus = claim.Status;

            claim.Status = ClaimStatus.Approved;
            claim.ApprovedAmount = request.Request.ApprovedAmount;
            claim.ReviewedBy = request.AdminEmail;
            claim.ReviewedByUserId = request.AdminId;
            claim.ReviewedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogActionAsync(
                action: "claim.approved",
                entityType: "Claim",
                entityId: request.ClaimId.ToString(),
                description: $"Claim approved for {request.Request.ApprovedAmount:C}",
                oldValue: new { status = oldStatus.ToString() },
                newValue: new { status = "Approved", approvedAmount = request.Request.ApprovedAmount },
                ct: ct);

            _logger.LogInformation("Claim {ClaimId} approved by {Admin} for amount {Amount}",
                request.ClaimId, request.AdminEmail, request.Request.ApprovedAmount);
        }
    }
}
