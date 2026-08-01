using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Common.Services
{
    public interface IPolicyCreationService
    {
        Task<InsurancePolicy> CreatePolicyAsync(
            Guid userId, Guid farmId, Guid insurancePlanId,
            DateTime startDate, DateTime? endDate, string? policyNumber,
            CancellationToken ct);
    }

    public class PolicyCreationService : IPolicyCreationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PolicyCreationService> _logger;

        public PolicyCreationService(
            IApplicationDbContext context,
            IUnitOfWork unitOfWork,
            ILogger<PolicyCreationService> logger)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<InsurancePolicy> CreatePolicyAsync(
            Guid userId, Guid farmId, Guid insurancePlanId,
            DateTime startDate, DateTime? endDate, string? policyNumber,
            CancellationToken ct)
        {
            _logger.LogInformation("Creating insurance policy for user: {UserId}", userId);

            var farm = await _context.Farms
                .FirstOrDefaultAsync(f => f.Id == farmId
                                          && f.UserId == userId
                                          && !f.IsDeleted, ct);

            if (farm == null)
                throw new NotFoundException(nameof(Farm), farmId);

            var plan = await _context.InsurancePlans
                .FirstOrDefaultAsync(p => p.Id == insurancePlanId
                                          && !p.IsDeleted
                                          && p.IsActive, ct);

            if (plan == null)
                throw new NotFoundException(nameof(InsurancePlan), insurancePlanId);

            if (!string.IsNullOrWhiteSpace(farm.CropType)
                && !string.IsNullOrWhiteSpace(plan.CropType)
                && !string.Equals(farm.CropType, plan.CropType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(new List<string>
                {
                    $"Plan crop type '{plan.CropType}' does not match farm crop type '{farm.CropType}'."
                });
            }

            if (plan.MinAreaInHectares.HasValue && farm.AreaInHectares < plan.MinAreaInHectares.Value)
                throw new ValidationException(new List<string>
                {
                    $"Farm area ({farm.AreaInHectares} ha) is below the plan minimum ({plan.MinAreaInHectares} ha)."
                });

            if (plan.MaxAreaInHectares.HasValue && farm.AreaInHectares > plan.MaxAreaInHectares.Value)
                throw new ValidationException(new List<string>
                {
                    $"Farm area ({farm.AreaInHectares} ha) exceeds the plan maximum ({plan.MaxAreaInHectares} ha)."
                });

            var computedEndDate = endDate ?? startDate.AddMonths(plan.PolicyDurationMonths);

            if (computedEndDate <= startDate)
                throw new ValidationException(new List<string>
                {
                    "End date must be after start date"
                });

            var computedPolicyNumber = string.IsNullOrWhiteSpace(policyNumber)
                ? GeneratePolicyNumber()
                : policyNumber.Trim();

            var duplicatePolicy = await _context.InsurancePolicies
                .AnyAsync(p => p.PolicyNumber == computedPolicyNumber && !p.IsDeleted, ct);

            if (duplicatePolicy)
                throw new ValidationException(new List<string>
                {
                    $"A policy with number '{computedPolicyNumber}' already exists"
                });

            var existingPolicy = await _context.InsurancePolicies
                .AnyAsync(p => p.FarmId == farmId
                               && (p.Status == PolicyStatus.Pending || p.Status == PolicyStatus.Active || p.Status == PolicyStatus.PaymentReceived)
                               && !p.IsDeleted, ct);

            if (existingPolicy)
                throw new ValidationException(new List<string>
                {
                    "This farm already has a pending, active, or payment-received policy. Wait for approval or cancel it first."
                });

            var area = farm.AreaInHectares;
            var sumInsured = plan.SumInsuredPerHectare * area;
            var coverageAmount = sumInsured * (plan.CoveragePercentage / 100m);
            var premium = plan.PremiumRatePerHectare * area;

            var schedules = new List<PremiumSchedule>();

            var policy = new InsurancePolicy
            {
                FarmId = farmId,
                InsurancePlanId = plan.Id,
                PolicyNumber = computedPolicyNumber,
                Provider = plan.Provider,
                CropType = plan.CropType,
                CoverageAmount = coverageAmount,
                Premium = premium,
                SumInsured = sumInsured,
                StartDate = startDate,
                EndDate = computedEndDate
            };

            if (plan.SupportsInstallments && plan.InstallmentCount.HasValue && plan.InstallmentCount.Value > 1)
            {
                var count = plan.InstallmentCount.Value;
                var baseInstallmentAmount = Math.Round(premium / count, 2);

                policy.InstallmentAmount = baseInstallmentAmount;
                policy.CurrentInstallmentNumber = 1;

                for (int i = 1; i <= count; i++)
                {
                    decimal amount = (i == count)
                        ? premium - (baseInstallmentAmount * (count - 1))
                        : baseInstallmentAmount;

                    DateTime dueDate = plan.InstallmentFrequency switch
                    {
                        InstallmentFrequency.Monthly => startDate.AddMonths(i - 1),
                        InstallmentFrequency.Quarterly => startDate.AddMonths((i - 1) * 3),
                        InstallmentFrequency.Annually => startDate.AddYears(i - 1),
                        _ => startDate.AddMonths(i - 1)
                    };

                    schedules.Add(new PremiumSchedule
                    {
                        PolicyId = policy.Id,
                        InstallmentNumber = i,
                        DueDate = dueDate,
                        AmountDue = amount,
                        Status = PremiumScheduleStatus.Pending
                    });
                }

                policy.NextInstallmentDueDate = schedules[0].DueDate;
                policy.PremiumSchedules = schedules;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _context.InsurancePolicies.AddAsync(policy, ct);
                if (schedules.Count > 0)
                    await _context.PremiumSchedules.AddRangeAsync(schedules, ct);
            }, ct);

            _logger.LogInformation(
                "Policy created: {PolicyId}, Number: {PolicyNumber}, Plan: {PlanId}, " +
                "Area: {Area} ha, Premium: {Premium}, SumInsured: {SumInsured}, Coverage: {Coverage}",
                policy.Id, policy.PolicyNumber, plan.Id,
                area, premium, sumInsured, coverageAmount);

            return policy;
        }

        private static string GeneratePolicyNumber()
        {
            var tag = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            return $"POL-{DateTime.UtcNow:yyyy}-{tag}";
        }
    }
}
