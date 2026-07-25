using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.AuditLogs.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAuditLogsQueryHandler> _logger;

        public GetAuditLogsQueryHandler(IApplicationDbContext context, ILogger<GetAuditLogsQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<AuditLogListDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
        {
            IQueryable<AuditLog> q = _context.AuditLogs.AsNoTracking();

            if (request.UserId.HasValue)
                q = q.Where(a => a.UserId == request.UserId.Value);

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                q = q.Where(a => a.EntityType == request.EntityType);

            if (!string.IsNullOrWhiteSpace(request.Action))
                q = q.Where(a => a.Action == request.Action);

            if (request.FromDate.HasValue)
                q = q.Where(a => a.Timestamp >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                q = q.Where(a => a.Timestamp <= request.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                q = q.Where(a =>
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(term)) ||
                    a.Action.ToLower().Contains(term) ||
                    a.EntityType.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            var totalCount = await q.CountAsync(ct);

            var logs = await q
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new AuditLogListDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Description = a.Description,
                    IpAddress = a.IpAddress,
                    Timestamp = a.Timestamp
                })
                .ToListAsync(ct);

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            _logger.LogInformation("Retrieved {Count} audit logs (Page {Page} of {Total})",
                logs.Count, request.PageNumber, totalPages);

            return new PagedResult<AuditLogListDto>
            {
                Items = logs,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}