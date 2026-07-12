using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Queries.GetMyPolicies
{
    public class GetMyPoliciesQueryHandler : IRequestHandler<GetMyPoliciesQuery, PagedResult<PolicyListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetMyPoliciesQueryHandler> _logger;

        public GetMyPoliciesQueryHandler(
            IApplicationDbContext context,
            ILogger<GetMyPoliciesQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<PolicyListDto>> Handle(GetMyPoliciesQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting policies for user {UserId}, Page: {Page}, Size: {Size}",
                request.UserId, request.PageNumber, request.PageSize);

            IQueryable<InsurancePolicy> queryable = _context.InsurancePolicies
                .AsNoTracking()
                .Include(p => p.Farm)
                .Include(p => p.Claims)
                .Where(p => p.Farm!.UserId == request.UserId && !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                queryable = queryable.Where(p =>
                    p.PolicyNumber.ToLower().Contains(term) ||
                    p.Provider.ToLower().Contains(term) ||
                    p.CropType.ToLower().Contains(term) ||
                    (p.Farm != null && p.Farm.Name.ToLower().Contains(term)));
            }

            var totalCount = await queryable.CountAsync(ct);

            var policies = await queryable
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = policies.Select(p => new PolicyListDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                Provider = p.Provider,
                CropType = p.CropType,
                CoverageAmount = p.CoverageAmount,
                Premium = p.Premium,
                SumInsured = p.SumInsured,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                FarmName = p.Farm?.Name,
                ClaimsCount = p.Claims.Count(c => !c.IsDeleted)
            }).ToList();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} policies (Page {Page} of {TotalPages})",
                items.Count, request.PageNumber, totalPages);

            return new PagedResult<PolicyListDto>
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