using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Queries.GetDashboardStats
{
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(IApplicationDbContext context, ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
        {
            // --- Claim counts (SQL aggregation) ---
            var totalClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted, ct);
            var pendingClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted && c.Status == ClaimStatus.Pending, ct);
            var approvedClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted && c.Status == ClaimStatus.Approved, ct);
            var rejectedClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted && c.Status == ClaimStatus.Rejected, ct);
            var underReviewClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted && c.Status == ClaimStatus.UnderReview, ct);
            var paidClaims = await _context.Claims.AsNoTracking().CountAsync(c => !c.IsDeleted && c.Status == ClaimStatus.Paid, ct);

            // --- Payout amounts (SQL aggregation) ---
            var totalPayoutAmount = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Status == ClaimStatus.Approved && c.ApprovedAmount.HasValue)
                .SumAsync(c => c.ApprovedAmount!.Value, ct);

            // Pending payout = sum of approved amounts for claims that are still pending (estimated)
            var pendingPayoutAmount = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Status == ClaimStatus.Pending && c.ApprovedAmount.HasValue)
                .SumAsync(c => c.ApprovedAmount!.Value, ct);

            // --- Claim metadata counts (SQL) ---
            var claimsWithImages = await _context.ClaimImages.AsNoTracking()
                .CountAsync(i => !i.IsDeleted && _context.Claims.Any(c => c.Id == i.ClaimId && !c.IsDeleted), ct);
            var claimsWithAIAnalysis = await _context.Claims.AsNoTracking()
                .CountAsync(c => !c.IsDeleted && c.AIAnalysisResult != null
                    && !c.AIAnalysisResult.Contains("\"isError\""), ct);
            var claimsWithWeatherData = await _context.Claims.AsNoTracking()
                .CountAsync(c => !c.IsDeleted && c.WeatherSnapshot != null, ct);

            // --- Average processing days (fetch only reviewed claims for averaging) ---
            var reviewedClaims = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Status == ClaimStatus.Approved && c.ReviewedAt.HasValue)
                .Select(c => new { c.CreatedAt, c.ReviewedAt })
                .ToListAsync(ct);

            var avgProcessingDays = reviewedClaims.Count > 0
                ? reviewedClaims.Average(c => (c.ReviewedAt!.Value - c.CreatedAt).TotalDays)
                : 0;

            // --- Policy counts (SQL aggregation) ---
            var totalPolicies = await _context.InsurancePolicies.AsNoTracking().CountAsync(p => !p.IsDeleted, ct);
            var pendingPolicies = await _context.InsurancePolicies.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PolicyStatus.Pending, ct);
            var activePolicies = await _context.InsurancePolicies.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PolicyStatus.Active, ct);
            var rejectedPolicies = await _context.InsurancePolicies.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PolicyStatus.Rejected, ct);
            var expiredPolicies = await _context.InsurancePolicies.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PolicyStatus.Expired, ct);

            // --- User & Farm counts (SQL aggregation) ---
            var totalFarmers = await _context.Users.AsNoTracking()
                .CountAsync(u => u.Role == UserRole.Farmer && !u.IsDeleted, ct);
            var totalFarms = await _context.Farms.AsNoTracking()
                .CountAsync(f => !f.IsDeleted, ct);

            // --- Incident breakdown (SQL GroupBy) ---
            var incidentBreakdown = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .GroupBy(c => c.IncidentType)
                .Select(g => new IncidentTypeBreakdown
                {
                    IncidentType = g.Key.ToString(),
                    Count = g.Count(),
                    TotalClaimed = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            // --- Monthly trends (SQL GroupBy — materialize raw, format in-memory) ---
            var monthlyTrendsData = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Claims = g.Count(),
                    Amount = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(12)
                .ToListAsync(ct);

            var monthlyTrends = monthlyTrendsData
                .Select(m => new MonthlyTrend
                {
                    Month = $"{m.Year}-{m.Month:D2}",
                    Claims = m.Claims,
                    Amount = m.Amount
                })
                .ToList();

            // --- Top farms by claim count (SQL GroupBy — FK only, no nav property) ---
            var topFarmAggs = await _context.Claims.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .GroupBy(c => c.FarmId)
                .Select(g => new
                {
                    FarmId = g.Key,
                    ClaimCount = g.Count(),
                    TotalClaimed = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                })
                .OrderByDescending(x => x.ClaimCount)
                .Take(5)
                .ToListAsync(ct);

            var topFarmIds = topFarmAggs.Select(t => t.FarmId).ToList();

            var farmInfos = await _context.Farms.AsNoTracking()
                .Where(f => topFarmIds.Contains(f.Id))
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    FarmerName = f.User != null ? f.User.FirstName + " " + f.User.LastName : ""
                })
                .ToDictionaryAsync(f => f.Id, ct);

            var topFarms = topFarmAggs.Select(t =>
            {
                farmInfos.TryGetValue(t.FarmId, out var info);
                return new TopFarmDto
                {
                    FarmId = t.FarmId,
                    FarmName = info?.Name ?? "",
                    FarmerName = info?.FarmerName ?? "",
                    ClaimCount = t.ClaimCount,
                    TotalClaimed = t.TotalClaimed
                };
            }).ToList();

            return new DashboardStatsDto
            {
                TotalClaims = totalClaims,
                PendingClaims = pendingClaims,
                ApprovedClaims = approvedClaims,
                RejectedClaims = rejectedClaims,
                UnderReviewClaims = underReviewClaims,
                PaidClaims = paidClaims,
                TotalPayoutAmount = totalPayoutAmount,
                PendingPayoutAmount = pendingPayoutAmount,
                ClaimsWithImages = claimsWithImages,
                ClaimsWithAIAnalysis = claimsWithAIAnalysis,
                ClaimsWithWeatherData = claimsWithWeatherData,
                AverageProcessingDays = (decimal)Math.Round(avgProcessingDays, 1),

                TotalPolicies = totalPolicies,
                PendingPolicies = pendingPolicies,
                ActivePolicies = activePolicies,
                RejectedPolicies = rejectedPolicies,
                ExpiredPolicies = expiredPolicies,

                TotalFarmers = totalFarmers,
                TotalFarms = totalFarms,

                IncidentBreakdown = incidentBreakdown,
                MonthlyTrends = monthlyTrends,
                TopFarms = topFarms
            };
        }
    }
}