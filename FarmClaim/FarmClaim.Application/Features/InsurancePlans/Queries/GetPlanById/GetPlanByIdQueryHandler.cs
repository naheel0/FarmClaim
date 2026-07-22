using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.InsurancePlans.Queries.GetPlanById
{
    public class GetPlanByIdQueryHandler : IRequestHandler<GetPlanByIdQuery, PlanResponseDto>
    {
        private readonly IApplicationDbContext _context;

        public GetPlanByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlanResponseDto> Handle(GetPlanByIdQuery request, CancellationToken ct)
        {
            var q = _context.InsurancePlans
                .AsNoTracking()
                .Include(p => p.Policies)
                .Where(p => p.Id == request.PlanId && !p.IsDeleted);

            if (!request.AdminContext)
                q = q.Where(p => p.IsActive);

            var plan = await q.FirstOrDefaultAsync(ct);

            if (plan == null)
                throw new NotFoundException(nameof(InsurancePlan), request.PlanId);

            return new PlanResponseDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                CropType = plan.CropType,
                Provider = plan.Provider,
                PremiumRatePerHectare = plan.PremiumRatePerHectare,
                SumInsuredPerHectare = plan.SumInsuredPerHectare,
                CoveragePercentage = plan.CoveragePercentage,
                MinAreaInHectares = plan.MinAreaInHectares,
                MaxAreaInHectares = plan.MaxAreaInHectares,
                PolicyDurationMonths = plan.PolicyDurationMonths,
                IsActive = plan.IsActive,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                PoliciesCount = plan.Policies.Count(p => !p.IsDeleted)
            };
        }
    }
}