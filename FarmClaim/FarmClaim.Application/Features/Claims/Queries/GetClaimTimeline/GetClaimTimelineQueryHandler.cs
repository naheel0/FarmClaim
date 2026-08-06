using System.Text.Json;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
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
                    Description = $"Claim created for {FormatIncidentType(claim.IncidentType)} incident on {claim.IncidentDate:yyyy-MM-dd}"
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

                var transition = GetStatusTransition(newValues, oldValues);
                if (transition == null)
                    continue;

                timeline.Add(new ClaimTimelineEntryDto
                {
                    Timestamp = log.Timestamp,
                    Action = transition.Value.Action,
                    Description = transition.Value.Description,
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
                    Description = $"Claim payment of {FormatINR(claim.ApprovedAmount)} was processed"
                });
            }

            return timeline;
        }

        private static (string Action, string Description)? GetStatusTransition(JsonElement? newValues, JsonElement? oldValues)
        {
            int? newStatus = ReadStatus(newValues);
            int? oldStatus = ReadStatus(oldValues);

            if (newStatus.HasValue && newStatus.Value == (int)ClaimStatus.UnderReview
                && (!oldStatus.HasValue || oldStatus.Value == (int)ClaimStatus.Pending))
            {
                return ("Under Review", "Claim is being reviewed by the administrator");
            }

            if (newStatus.HasValue && newStatus.Value == (int)ClaimStatus.Approved
                && (!oldStatus.HasValue || oldStatus.Value != (int)ClaimStatus.Approved))
            {
                return ("Approved", ReadAmount(newValues) is decimal amt
                    ? $"Claim approved for {FormatINR(amt)}"
                    : "Claim has been approved");
            }

            if (newStatus.HasValue && newStatus.Value == (int)ClaimStatus.Rejected
                && (!oldStatus.HasValue || oldStatus.Value != (int)ClaimStatus.Rejected))
            {
                var reason = ReadString(newValues, "RejectionReason");
                return ("Claim Rejected", string.IsNullOrEmpty(reason) ? "Claim was rejected" : $"Rejected: {reason}");
            }

            if (newStatus.HasValue && newStatus.Value == (int)ClaimStatus.Paid
                && (!oldStatus.HasValue || oldStatus.Value != (int)ClaimStatus.Paid))
            {
                return ("Payment Processed", "Claim payment has been disbursed");
            }

            return null;
        }

        private static int? ReadStatus(JsonElement? element)
        {
            if (element is not JsonElement root || !root.TryGetProperty("Status", out var status))
                return null;
            if (status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var intStatus))
                return intStatus;
            if (status.ValueKind == JsonValueKind.String
                && Enum.TryParse<ClaimStatus>(status.GetString(), ignoreCase: true, out var parsed))
                return (int)parsed;
            return null;
        }

        private static string? ReadString(JsonElement? element, string property)
        {
            if (element is not JsonElement root || !root.TryGetProperty(property, out var value))
                return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static decimal? ReadAmount(JsonElement? element)
        {
            if (element is not JsonElement root)
                return null;
            if (root.TryGetProperty("ApprovedAmount", out var amount)
                && (amount.ValueKind == JsonValueKind.Number || decimal.TryParse(amount.ToString(), out _)))
            {
                return (decimal)amount.GetDouble();
            }
            return null;
        }

        private static string FormatINR(decimal? amount)
        {
            if (!amount.HasValue) return "—";
            return $"₹{amount.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        private static string FormatIncidentType(IncidentType incidentType)
        {
            return System.Text.RegularExpressions.Regex.Replace(incidentType.ToString(), "([A-Z])", " $1").Trim();
        }
    }
}
