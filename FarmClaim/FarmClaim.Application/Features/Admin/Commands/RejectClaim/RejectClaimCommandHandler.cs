using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.RejectClaim
{
    public class RejectClaimCommandHandler : IRequestHandler<RejectClaimCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<RejectClaimCommandHandler> _logger;

        public RejectClaimCommandHandler(
            IApplicationDbContext context,
            IAuditService auditService,
            ILogger<RejectClaimCommandHandler> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(RejectClaimCommand request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException("Claim not found");

            if (claim.Status == ClaimStatus.Approved)
                throw new ValidationException(new List<string> { "Cannot reject an approved claim" });

            if (claim.Status == ClaimStatus.Rejected)
                throw new ValidationException(new List<string> { "Claim is already rejected" });

            if (claim.Status == ClaimStatus.Paid)
                throw new ValidationException(new List<string> { "Cannot reject a paid claim" });

            var oldStatus = claim.Status;

            claim.Status = ClaimStatus.Rejected;
            claim.RejectionReason = request.Request.RejectionReason?.Trim();
            claim.ReviewedBy = request.AdminEmail;
            claim.ReviewedByUserId = request.AdminId;
            claim.ReviewedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogActionAsync(
                action: "claim.rejected",
                entityType: "Claim",
                entityId: request.ClaimId.ToString(),
                description: $"Claim rejected: {request.Request.RejectionReason}",
                oldValue: new { status = oldStatus.ToString() },
                newValue: new { status = "Rejected", reason = request.Request.RejectionReason },
                ct: ct);

            _logger.LogInformation("Claim {ClaimId} rejected by {Admin}. Reason: {Reason}",
                request.ClaimId, request.AdminEmail, request.Request.RejectionReason);
        }
    }
}
