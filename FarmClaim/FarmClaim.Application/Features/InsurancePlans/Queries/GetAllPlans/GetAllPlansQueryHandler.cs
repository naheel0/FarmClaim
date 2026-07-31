using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans
{
    public class GetAllPlansQueryHandler : IRequestHandler<GetAllPlansQuery, PagedResult<PlanListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAllPlansQueryHandler> _logger;

        public GetAllPlansQueryHandler(
            IApplicationDbContext context,
            ILogger<GetAllPlansQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<PlanListDto>> Handle(GetAllPlansQuery request, CancellationToken ct)
        {
            IQueryable<InsurancePlan> q = _context.InsurancePlans
                .AsNoTracking()
                .Include(p => p.Policies)
                .Where(p => !p.IsDeleted);

            if (!request.AdminContext)
                q = q.Where(p => p.IsActive);

            if (request.IsActive.HasValue && request.AdminContext)
                q = q.Where(p => p.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.CropType))
            {
                var crop = request.CropType.Trim().ToLower();
                q = q.Where(p => p.CropType.ToLower() == crop);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                q = q.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Provider.ToLower().Contains(term) ||
                    p.CropType.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(ct);

            var plans = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = plans.Select(p => new PlanListDto
            {
                Id = p.Id,
                Name = p.Name,
                CropType = p.CropType,
                Provider = p.Provider,
                PremiumRatePerHectare = p.PremiumRatePerHectare,
                SumInsuredPerHectare = p.SumInsuredPerHectare,
                CoveragePercentage = p.CoveragePercentage,
                PolicyDurationMonths = p.PolicyDurationMonths,
                IsActive = p.IsActive,
                PoliciesCount = p.Policies.Count(pl => !pl.IsDeleted),
                SupportsInstallments = p.SupportsInstallments,
                InstallmentCount = p.InstallmentCount,
                InstallmentFrequency = p.InstallmentFrequency,
                CreatedAt = p.CreatedAt
            }).ToList();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} plans (Page {Page} of {TotalPages})",
                items.Count, request.PageNumber, totalPages);

            return new PagedResult<PlanListDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}