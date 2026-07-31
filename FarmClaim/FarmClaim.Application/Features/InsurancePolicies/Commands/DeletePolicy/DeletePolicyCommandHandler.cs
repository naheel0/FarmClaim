using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.DeletePolicy
{
    public class DeletePolicyCommandHandler : IRequestHandler<DeletePolicyCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DeletePolicyCommandHandler> _logger;

        public DeletePolicyCommandHandler(
            IApplicationDbContext context,
            ILogger<DeletePolicyCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(DeletePolicyCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Deleting policy: {PolicyId} for user: {UserId}", command.PolicyId, command.UserId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .FirstOrDefaultAsync(p => p.Id == command.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && !p.IsDeleted, ct);

            if (policy == null)
            {
                _logger.LogWarning("Policy not found: {PolicyId} or not owned by user: {UserId}", command.PolicyId, command.UserId);
                throw new NotFoundException(nameof(InsurancePolicy), command.PolicyId);
            }

            var hasActiveClaims = await _context.Claims
                .AnyAsync(c => c.PolicyId == command.PolicyId
                    && !c.IsDeleted
                    && c.Status != ClaimStatus.Pending, ct);

            if (hasActiveClaims)
                throw new ValidationException(new List<string> { "Cannot delete policy with active claims. Please resolve all claims first." });

            // Block deletion if payment was captured or is in progress
            var hasCapturedPayment = await _context.Payments
                .AnyAsync(p => p.PolicyId == command.PolicyId
                    && p.Status == PaymentStatus.Captured
                    && !p.IsDeleted, ct);

            if (hasCapturedPayment)
                throw new ValidationException(new List<string> { "Cannot delete policy with confirmed payment. Process a refund first." });

            var hasPendingPayment = await _context.Payments
                .AnyAsync(p => p.PolicyId == command.PolicyId
                    && (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.Attempted)
                    && !p.IsDeleted, ct);

            if (hasPendingPayment)
                throw new ValidationException(new List<string> { "Cannot delete policy while payment is in progress. Wait for payment to complete or expire." });

            policy.IsDeleted = true;
            policy.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Policy deleted: {PolicyId}", policy.Id);
            return Unit.Value;
        }
    }
}