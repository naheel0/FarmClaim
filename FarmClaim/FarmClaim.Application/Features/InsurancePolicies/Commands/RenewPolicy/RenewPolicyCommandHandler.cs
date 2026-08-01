using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Common.Services;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.RenewPolicy
{
    public class RenewPolicyCommandHandler : IRequestHandler<RenewPolicyCommand, PolicyResponseDto>
    {
        private readonly IPolicyCreationService _policyCreationService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<RenewPolicyCommandHandler> _logger;

        public RenewPolicyCommandHandler(
            IPolicyCreationService policyCreationService,
            IApplicationDbContext context,
            ILogger<RenewPolicyCommandHandler> logger)
        {
            _policyCreationService = policyCreationService;
            _context = context;
            _logger = logger;
        }

        public async Task<PolicyResponseDto> Handle(RenewPolicyCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Renewing policy {PolicyId} for user {UserId}", command.PolicyId, command.UserId);

            var oldPolicy = await _context.InsurancePolicies
                .AsNoTracking()
                .Include(p => p.Farm)
                .Include(p => p.InsurancePlan)
                .FirstOrDefaultAsync(p => p.Id == command.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && !p.IsDeleted, ct);

            if (oldPolicy == null)
                throw new NotFoundException(nameof(InsurancePolicy), command.PolicyId);

            if (oldPolicy.Status != PolicyStatus.Expired)
                throw new ValidationException(new List<string>
                {
                    $"Only expired policies can be renewed. Current status: {oldPolicy.Status}."
                });

            if (!oldPolicy.InsurancePlanId.HasValue)
                throw new ValidationException(new List<string>
                {
                    "Cannot renew a policy that is not linked to an insurance plan."
                });

            var startDate = command.StartDate ?? oldPolicy.EndDate;
            if (startDate < DateTime.UtcNow)
                startDate = DateTime.UtcNow;

            var policy = await _policyCreationService.CreatePolicyAsync(
                command.UserId,
                oldPolicy.FarmId,
                oldPolicy.InsurancePlanId.Value,
                startDate,
                endDate: null,
                policyNumber: null,
                ct);

            var farm = await _context.Farms
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == policy.FarmId && !f.IsDeleted, ct);

            return new PolicyResponseDto
            {
                Id = policy.Id,
                FarmId = policy.FarmId,
                UserId = command.UserId,
                FarmName = farm?.Name ?? string.Empty,
                PolicyNumber = policy.PolicyNumber,
                Provider = policy.Provider,
                CropType = policy.CropType,
                CoverageAmount = policy.CoverageAmount,
                Premium = policy.Premium,
                SumInsured = policy.SumInsured,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                Status = policy.Status,
                CurrentInstallmentNumber = policy.CurrentInstallmentNumber,
                NextInstallmentDueDate = policy.NextInstallmentDueDate,
                InstallmentAmount = policy.InstallmentAmount,
                PremiumSchedules = policy.PremiumSchedules?.Select(s => new PremiumScheduleDto
                {
                    Id = s.Id,
                    PolicyId = s.PolicyId,
                    InstallmentNumber = s.InstallmentNumber,
                    DueDate = s.DueDate,
                    AmountDue = s.AmountDue,
                    PaymentId = s.PaymentId,
                    Status = s.Status,
                    PaidAt = s.PaidAt
                }).ToList(),
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
                ClaimsCount = 0
            };
        }
    }
}
