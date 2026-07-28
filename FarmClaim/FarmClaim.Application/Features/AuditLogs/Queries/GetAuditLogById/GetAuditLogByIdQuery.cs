using FarmClaim.Application.Features.AuditLogs.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.AuditLogs.Queries.GetAuditLogById
{
    public record GetAuditLogByIdQuery(Guid LogId) : IRequest<AuditLogDetailDto>;
}