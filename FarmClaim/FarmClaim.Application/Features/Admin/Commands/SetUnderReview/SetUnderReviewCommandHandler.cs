using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.SetUnderReview
{
    public class SetUnderReviewCommandHandler : IRequestHandler<SetUnderReviewCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<SetUnderReviewCommandHandler> _logger;

        public SetUnderReviewCommandHandler(
            IApplicationDbContext context,
            IAuditService auditService,
            ILogger<SetUnderReviewCommandHandler> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(SetUnderReviewCommand request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException("Claim not found");

            if (claim.Status != ClaimStatus.Pending)
                throw new ValidationException(new List<string>
                {
                    $"Only pending claims can be set to review. Current: {claim.Status}"
                });

            claim.Status = ClaimStatus.UnderReview;
            claim.ReviewedBy = request.AdminEmail;
            claim.ReviewedByUserId = request.AdminId;
            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogActionAsync(
                action: "claim.under_review",
                entityType: "Claim",
                entityId: request.ClaimId.ToString(),
                description: $"Claim set to under review by {request.AdminEmail}",
                ct: ct);

            _logger.LogInformation("Claim {ClaimId} set to under review by {Admin}", request.ClaimId, request.AdminEmail);
        }
    }
}
