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
        private readonly ILogger<ApprovePolicyCommandHandler> _logger;

        public ApprovePolicyCommandHandler(
            IApplicationDbContext context,
            ILogger<ApprovePolicyCommandHandler> logger)
        {
            _context = context;
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

            if (policy.Status != PolicyStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot approve. Policy status is '{policy.Status}'. Only 'Pending' policies can be approved.");

            policy.Status = PolicyStatus.Active;
            policy.ApprovedAt = DateTime.UtcNow;
            policy.ApprovedByUserId = request.AdminUserId;

            await _context.SaveChangesAsync(ct);

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