using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farms.DTOs;
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
            _logger.LogInformation("Getting farms for user: {UserId}", request.UserId);

            var query = _context.Farms
                .AsNoTracking()
                .Where(f => f.UserId == request.UserId && !f.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(f =>
                    f.Name.Contains(request.SearchTerm) ||
                    (f.Address != null && f.Address.Contains(request.SearchTerm)));
            }

            var totalItems = await query.CountAsync(ct);

            var farms = await query
                .Include(f => f.InsurancePolicies)
                .Include(f => f.Claims)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = farms.Select(f => new FarmListDto
            {
                Id = f.Id,
                Name = f.Name,
                AreaInHectares = f.AreaInHectares,
                Address = f.Address,
                Latitude = f.Latitude,
                Longitude = f.Longitude,
                IsActive = f.IsActive,
                PoliciesCount = f.InsurancePolicies.Count(p => !p.IsDeleted),
                ClaimsCount = f.Claims.Count(c => !c.IsDeleted),
                CreatedAt = f.CreatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} farms for user: {UserId}", items.Count, request.UserId);

            return new PagedResult<FarmListDto>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}