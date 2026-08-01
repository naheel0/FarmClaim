using System.Text.Json;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Queries.GetClaimTimeline
{
    public class GetClaimTimelineQueryHandler : IRequestHandler<GetClaimTimelineQuery, List<ClaimTimelineEntryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetClaimTimelineQueryHandler> _logger;

        public GetClaimTimelineQueryHandler(
            IApplicationDbContext context,
            ILogger<GetClaimTimelineQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ClaimTimelineEntryDto>> Handle(GetClaimTimelineQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting timeline for claim {ClaimId} by user {UserId}", request.ClaimId, request.UserId);

            var claim = await _context.Claims
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId
                    && c.UserId == request.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException(nameof(Claim), request.ClaimId);

            var entityId = request.ClaimId.ToString();

            var auditLogs = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.EntityType == "Claim"
                    && a.EntityId == entityId
                    && a.Timestamp >= claim.CreatedAt.AddSeconds(-1))
                .OrderBy(a => a.Timestamp)
                .ToListAsync(ct);

            var timeline = new List<ClaimTimelineEntryDto>
            {
                new ClaimTimelineEntryDto
                {
                    Timestamp = claim.CreatedAt,
                    Action = "Claim Submitted",
                    Description = $"Claim created for {claim.IncidentType} incident on {claim.IncidentDate:yyyy-MM-dd}"
                }
            };

            foreach (var log in auditLogs)
            {
                JsonElement? oldValues = null;
                JsonElement? newValues = null;

                if (!string.IsNullOrEmpty(log.OldValues))
                {
                    try { oldValues = JsonDocument.Parse(log.OldValues).RootElement; }
                    catch { /* skip malformed JSON */ }
                }

                if (!string.IsNullOrEmpty(log.NewValues))
                {
                    try { newValues = JsonDocument.Parse(log.NewValues).RootElement; }
                    catch { /* skip malformed JSON */ }
                }

                timeline.Add(new ClaimTimelineEntryDto
                {
                    Timestamp = log.Timestamp,
                    Action = FormatAction(log.Action),
                    Description = log.Description,
                    OldValues = oldValues,
                    NewValues = newValues,
                    ChangedColumns = log.ChangedColumns
                });
            }

            if (claim.PaidAt.HasValue)
            {
                timeline.Add(new ClaimTimelineEntryDto
                {
                    Timestamp = claim.PaidAt.Value,
                    Action = "Payment Processed",
                    Description = $"Claim payment of {claim.ApprovedAmount:C} was processed"
                });
            }

            return timeline;
        }

        private static string FormatAction(string action)
        {
            return action switch
            {
                "claim.created" => "Claim Created",
                "claim.under_review" => "Under Review",
                "claim.approved" => "Claim Approved",
                "claim.rejected" => "Claim Rejected",
                "claim.paid" => "Claim Paid",
                "claim.images_uploaded" => "Images Uploaded",
                "claim.updated" => "Claim Updated",
                _ => action
            };
        }
    }
}
