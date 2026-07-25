using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.AuditLogs.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public record GetAuditLogsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        Guid? UserId = null,
        string? EntityType = null,
        string? Action = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        string? SearchTerm = null
    ) : IRequest<PagedResult<AuditLogListDto>>;
}