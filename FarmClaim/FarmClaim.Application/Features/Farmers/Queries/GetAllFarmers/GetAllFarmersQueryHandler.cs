using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farmers.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farmers.Queries.GetAllFarmers
{
    public class GetAllFarmersQueryHandler : IRequestHandler<GetAllFarmersQuery, PagedResult<FarmerListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAllFarmersQueryHandler> _logger;

        public GetAllFarmersQueryHandler(
            IApplicationDbContext context,
            ILogger<GetAllFarmersQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<FarmerListDto>> Handle(GetAllFarmersQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting all farmers, Page: {Page}, Size: {Size}",
                request.PageNumber, request.PageSize);

            IQueryable<User> queryable = _context.Users
                .AsNoTracking()
                .Include(u => u.Farms)
                .Include(u => u.Policies)
                .Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                queryable = queryable.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var totalCount = await queryable.CountAsync(ct);

            var users = await queryable
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = users.Select(u => new FarmerListDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                IsActive = !u.IsDeleted,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                FarmsCount = u.Farms.Count(f => !f.IsDeleted),
                PoliciesCount = u.Policies.Count(p => !p.IsDeleted && p.IsActive)
            }).ToList();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} farmers (Page {Page} of {TotalPages})",
                items.Count, request.PageNumber, totalPages);

            return new PagedResult<FarmerListDto>
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