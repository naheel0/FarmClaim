using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.CreatePolicy
{
    public class CreatePolicyCommandHandler : IRequestHandler<CreatePolicyCommand, PolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CreatePolicyCommandHandler> _logger;

        public CreatePolicyCommandHandler(
            IApplicationDbContext context,
            ILogger<CreatePolicyCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PolicyResponseDto> Handle(CreatePolicyCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Creating insurance policy for user: {UserId}", command.UserId);

            var farm = await _context.Farms
                .FirstOrDefaultAsync(f => f.Id == command.Request.FarmId
                                          && f.UserId == command.UserId
                                          && !f.IsDeleted, ct);

            if (farm == null)
                throw new NotFoundException(nameof(Farm), command.Request.FarmId);

            var plan = await _context.InsurancePlans
                .FirstOrDefaultAsync(p => p.Id == command.Request.InsurancePlanId
                                          && !p.IsDeleted
                                          && p.IsActive, ct);

            if (plan == null)
                throw new NotFoundException(nameof(InsurancePlan), command.Request.InsurancePlanId);

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

            var startDate = command.Request.StartDate;
            var endDate = command.Request.EndDate ?? startDate.AddMonths(plan.PolicyDurationMonths);

            if (endDate <= startDate)
                throw new ValidationException(new List<string>
                {
                    "End date must be after start date"
                });

            var policyNumber = string.IsNullOrWhiteSpace(command.Request.PolicyNumber)
                ? GeneratePolicyNumber()
                : command.Request.PolicyNumber.Trim();

            var duplicatePolicy = await _context.InsurancePolicies
                .AnyAsync(p => p.PolicyNumber == policyNumber && !p.IsDeleted, ct);

            if (duplicatePolicy)
                throw new ValidationException(new List<string>
                {
                    $"A policy with number '{policyNumber}' already exists"
                });

            var existingPolicy = await _context.InsurancePolicies
                .AnyAsync(p => p.FarmId == command.Request.FarmId
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

            var policy = new InsurancePolicy
            {
                FarmId = command.Request.FarmId,
                InsurancePlanId = plan.Id,
                PolicyNumber = policyNumber,
                Provider = plan.Provider,
                CropType = plan.CropType,
                CoverageAmount = coverageAmount,
                Premium = premium,
                SumInsured = sumInsured,
                StartDate = startDate,
                EndDate = endDate
            };

            await _context.InsurancePolicies.AddAsync(policy, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Policy created: {PolicyId}, Number: {PolicyNumber}, Plan: {PlanId}, " +
                "Area: {Area} ha, Premium: {Premium}, SumInsured: {SumInsured}, Coverage: {Coverage}",
                policy.Id, policy.PolicyNumber, plan.Id,
                area, premium, sumInsured, coverageAmount);

            return new PolicyResponseDto
            {
                Id = policy.Id,
                FarmId = policy.FarmId,
                UserId = command.UserId,
                FarmName = farm.Name,
                PolicyNumber = policy.PolicyNumber,
                Provider = policy.Provider,
                CropType = policy.CropType,
                CoverageAmount = policy.CoverageAmount,
                Premium = policy.Premium,
                SumInsured = policy.SumInsured,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                Status = policy.Status,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
                ClaimsCount = 0
            };
        }

        private static string GeneratePolicyNumber()
        {
            var tag = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            return $"POL-{DateTime.UtcNow:yyyy}-{tag}";
        }
    }
}