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
        private readonly ILogger<RejectPolicyCommandHandler> _logger;

        public RejectPolicyCommandHandler(
            IApplicationDbContext context,
            ILogger<RejectPolicyCommandHandler> logger)
        {
            _context = context;
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

            if (policy.Status != PolicyStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot reject. Policy status is '{policy.Status}'. Only 'Pending' policies can be rejected.");

            policy.Status = PolicyStatus.Rejected;
            policy.RejectedAt = DateTime.UtcNow;
            policy.RejectionReason = request.Request.Reason.Trim();

            await _context.SaveChangesAsync(ct);

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