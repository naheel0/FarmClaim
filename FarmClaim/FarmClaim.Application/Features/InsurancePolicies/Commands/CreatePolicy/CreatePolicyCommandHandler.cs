using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
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
                .FirstOrDefaultAsync(f => f.Id == command.Request.FarmId && f.UserId == command.UserId && !f.IsDeleted, ct);

            if (farm == null)
                throw new NotFoundException(nameof(Farm), command.Request.FarmId);

            var duplicatePolicy = await _context.InsurancePolicies
                .AnyAsync(p => p.PolicyNumber == command.Request.PolicyNumber && !p.IsDeleted, ct);

            if (duplicatePolicy)
                throw new ValidationException(new List<string> { $"A policy with number '{command.Request.PolicyNumber}' already exists" });

            var policy = new InsurancePolicy
            {
                FarmId = command.Request.FarmId,
                PolicyNumber = command.Request.PolicyNumber.Trim(),
                Provider = command.Request.Provider.Trim(),
                CropType = command.Request.CropType.Trim(),
                CoverageAmount = command.Request.CoverageAmount,
                Premium = command.Request.Premium,
                SumInsured = command.Request.SumInsured,
                StartDate = command.Request.StartDate,
                EndDate = command.Request.EndDate,
                IsActive = true
            };

            await _context.InsurancePolicies.AddAsync(policy, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Insurance policy created: {PolicyId}, Number: {PolicyNumber}",
                policy.Id, policy.PolicyNumber);

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
                IsActive = policy.IsActive,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
                ClaimsCount = 0
            };
        }
    }
}