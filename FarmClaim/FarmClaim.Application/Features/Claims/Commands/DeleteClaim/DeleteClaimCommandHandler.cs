using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.DeleteClaim
{
    public class DeleteClaimCommandHandler : IRequestHandler<DeleteClaimCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DeleteClaimCommandHandler> _logger;

        public DeleteClaimCommandHandler(
            IApplicationDbContext context,
            ILogger<DeleteClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(DeleteClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Deleting claim: {ClaimId} for user: {UserId}", command.ClaimId, command.UserId);

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == command.ClaimId
                    && c.UserId == command.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
            {
                _logger.LogWarning("Claim not found: {ClaimId} or not owned by user: {UserId}", command.ClaimId, command.UserId);
                throw new NotFoundException(nameof(Claim), command.ClaimId);
            }

            // Only allow deleting pending claims
            if (claim.Status != "Pending")
                throw new ValidationException(new List<string> { $"Cannot delete claim with status '{claim.Status}'. Only pending claims can be deleted." });

            claim.IsDeleted = true;
            claim.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Claim deleted: {ClaimId}", claim.Id);

            return Unit.Value;
        }
    }
}