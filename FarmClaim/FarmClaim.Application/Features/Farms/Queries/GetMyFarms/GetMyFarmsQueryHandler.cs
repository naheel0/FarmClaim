using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farms.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farms.Queries.GetMyFarms
{
    public class GetMyFarmsQueryHandler : IRequestHandler<GetMyFarmsQuery, PagedResult<FarmListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetMyFarmsQueryHandler> _logger;

        public GetMyFarmsQueryHandler(
            IApplicationDbContext context,
            ILogger<GetMyFarmsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<FarmListDto>> Handle(GetMyFarmsQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting farms for user {UserId}, Page: {Page}, Size: {Size}",
                request.UserId, request.PageNumber, request.PageSize);

            // Build queryable - start with base query (all user's active farms)
            IQueryable<Farm> queryable = _context.Farms
                .AsNoTracking()
                .Include(f => f.InsurancePolicies)
                .Include(f => f.Claims)
                .Where(f => f.UserId == request.UserId && !f.IsDeleted);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                queryable = queryable.Where(f =>
                    f.Name.ToLower().Contains(term) ||
                    (f.Address != null && f.Address.ToLower().Contains(term)));
            }

            // Get total count before pagination
            var totalCount = await queryable.CountAsync(ct);

            // Apply pagination
            var farms = await queryable
                .OrderByDescending(f => f.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            // Project to DTOs client-side to avoid expression tree limitations with null propagating operator
            var items = farms.Select(f => new FarmListDto
            {
                Id = f.Id,
                Name = f.Name,
                AreaInHectares = f.AreaInHectares,
                Address = f.Address,
                Latitude = f.Latitude,
                Longitude = f.Longitude,
                CreatedAt = f.CreatedAt,
                IsActive = f.IsActive,
                PoliciesCount = f.InsurancePolicies.Count(p => !p.IsDeleted && p.IsActive),
                ClaimsCount = f.Claims.Count(c => !c.IsDeleted)
            })
                .ToList();

            // Calculate total pages safely
            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} farms (Page {Page} of {TotalPages})",
                items.Count, request.PageNumber, request.UserId);

            return new PagedResult<FarmListDto>
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