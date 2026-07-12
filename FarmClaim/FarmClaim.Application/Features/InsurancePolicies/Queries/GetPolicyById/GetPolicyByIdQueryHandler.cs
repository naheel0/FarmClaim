using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Queries.GetPolicyById
{
    public class GetPolicyByIdQueryHandler : IRequestHandler<GetPolicyByIdQuery, PolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetPolicyByIdQueryHandler> _logger;

        public GetPolicyByIdQueryHandler(
            IApplicationDbContext context,
            ILogger<GetPolicyByIdQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PolicyResponseDto> Handle(GetPolicyByIdQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting policy {PolicyId} for user {UserId}", request.PolicyId, request.UserId);

            var policy = await _context.InsurancePolicies
                .AsNoTracking()
                .Include(p => p.Farm)
                .Include(p => p.Claims)
                .FirstOrDefaultAsync(p => p.Id == request.PolicyId && !p.IsDeleted, ct);

            if (policy == null)
            {
                _logger.LogWarning("Policy not found: {PolicyId}", request.PolicyId);
                throw new NotFoundException(nameof(InsurancePolicy), request.PolicyId);
            }

            _logger.LogInformation("Policy {PolicyId} retrieved successfully", policy.Id);

            return new PolicyResponseDto
            {
                Id = policy.Id,
                FarmId = policy.FarmId,
                UserId = policy.Farm!.UserId,
                FarmName = policy.Farm.Name,
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
                ClaimsCount = policy.Claims.Count(c => !c.IsDeleted)
            };
        }
    }
}