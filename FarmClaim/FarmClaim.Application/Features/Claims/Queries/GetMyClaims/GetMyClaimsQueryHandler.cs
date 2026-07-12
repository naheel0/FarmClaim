using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Queries.GetMyClaims
{
    public class GetMyClaimsQueryHandler : IRequestHandler<GetMyClaimsQuery, PagedResult<ClaimListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetMyClaimsQueryHandler> _logger;

        public GetMyClaimsQueryHandler(
            IApplicationDbContext context,
            ILogger<GetMyClaimsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<ClaimListDto>> Handle(GetMyClaimsQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting claims for user {UserId}, Page: {Page}, Size: {Size}",
                request.UserId, request.PageNumber, request.PageSize);

            IQueryable<Claim> queryable = _context.Claims
                .AsNoTracking()
                .Include(c => c.Policy)
                .Include(c => c.Farm)
                .Include(c => c.Images)
                .Where(c => c.UserId == request.UserId && !c.IsDeleted);

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                var status = request.StatusFilter.Trim();
                queryable = queryable.Where(c => c.Status == status);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                queryable = queryable.Where(c =>
                    c.IncidentType.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)) ||
                    (c.DamageDescription != null && c.DamageDescription.ToLower().Contains(term)) ||
                    (c.Policy != null && c.Policy.PolicyNumber.ToLower().Contains(term)) ||
                    (c.Farm != null && c.Farm.Name.ToLower().Contains(term)));
            }

            var totalCount = await queryable.CountAsync(ct);

            var claims = await queryable
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = claims.Select(c => new ClaimListDto
            {
                Id = c.Id,
                PolicyId = c.PolicyId,
                FarmId = c.FarmId,
                PolicyNumber = c.Policy?.PolicyNumber ?? string.Empty,
                FarmName = c.Farm?.Name ?? string.Empty,
                IncidentDate = c.IncidentDate,
                IncidentType = c.IncidentType,
                Status = c.Status,
                ApprovedAmount = c.ApprovedAmount,
                CreatedAt = c.CreatedAt,
                ImageCount = c.Images.Count(i => !i.IsDeleted)
            }).ToList();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} claims (Page {Page} of {TotalPages})",
                items.Count, request.PageNumber, totalPages);

            return new PagedResult<ClaimListDto>
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