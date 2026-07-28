using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.AuditLogs.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.AuditLogs.Queries.GetAuditLogById
{
    public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetAuditLogByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogDetailDto> Handle(GetAuditLogByIdQuery request, CancellationToken ct)
        {
            var log = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.Id == request.LogId)
                .Select(a => new AuditLogDetailDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    ChangedColumns = a.ChangedColumns,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    Description = a.Description,
                    Timestamp = a.Timestamp,
                    CorrelationId = a.CorrelationId,
                    HttpMethod = a.HttpMethod,
                    HttpPath = a.HttpPath
                })
                .FirstOrDefaultAsync(ct);

            if (log == null)
                throw new NotFoundException(nameof(AuditLog), request.LogId);

            return log;
        }
    }
}