using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagedResult<AdminUserListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAllUsersQueryHandler> _logger;

        public GetAllUsersQueryHandler(
            IApplicationDbContext context,
            ILogger<GetAllUsersQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<AdminUserListDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Admin listing users. Page: {Page}, Size: {Size}",
                request.PageNumber, request.PageSize);

            IQueryable<User> q = _context.Users.AsNoTracking();

            if (request.Role.HasValue)
                q = q.Where(u => u.Role == request.Role.Value);

            if (request.Status.HasValue)
                q = q.Where(u => u.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                q = q.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var sortBy = (request.SortBy ?? "CreatedAt").ToLower();
            var descending = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            q = (sortBy, descending) switch
            {
                ("email", true) => q.OrderByDescending(u => u.Email),
                ("email", false) => q.OrderBy(u => u.Email),
                ("firstname", true) => q.OrderByDescending(u => u.FirstName),
                ("firstname", false) => q.OrderBy(u => u.FirstName),
                ("lastname", true) => q.OrderByDescending(u => u.LastName),
                ("lastname", false) => q.OrderBy(u => u.LastName),
                ("role", true) => q.OrderByDescending(u => u.Role),
                ("role", false) => q.OrderBy(u => u.Role),
                ("status", true) => q.OrderByDescending(u => u.Status),
                ("status", false) => q.OrderBy(u => u.Status),
                ("lastloginat", true) => q.OrderByDescending(u => u.LastLoginAt),
                ("lastloginat", false) => q.OrderBy(u => u.LastLoginAt),
                _ => q.OrderByDescending(u => u.CreatedAt)
            };

            var totalCount = await q.CountAsync(ct);

            var users = await q
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var items = users.Select(u => new AdminUserListDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                Status = u.Status,
                LastLoginAt = u.LastLoginAt,
                StatusChangedAt = u.StatusChangedAt,
                StatusChangeReason = u.StatusChangeReason,
                CreatedAt = u.CreatedAt
            }).ToList();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            return new PagedResult<AdminUserListDto>
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