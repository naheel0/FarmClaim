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
            var claims = await _context.Claims
                .AsNoTracking()
                .Include(c => c.Policy)
                .Include(c => c.Farm).ThenInclude(f => f!.User)
                .Include(c => c.Images)
                .Where(c => !c.IsDeleted)
                .ToListAsync(ct);

            var approved = claims.Where(c => c.Status == ClaimStatus.Approved).ToList();
            var pending = claims.Where(c => c.Status == ClaimStatus.Pending).ToList();

            // NEW: Policy stats
            var policies = await _context.InsurancePolicies
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .ToListAsync(ct);

            // NEW: User & Farm stats
            var totalFarmers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.Role == UserRole.Farmer && !u.IsDeleted, ct);

            var totalFarms = await _context.Farms
                .AsNoTracking()
                .CountAsync(f => !f.IsDeleted, ct);

            var incidentBreakdown = claims
                .GroupBy(c => c.IncidentType.ToString())
                .Select(g => new IncidentTypeBreakdown
                {
                    IncidentType = g.Key,
                    Count = g.Count(),
                    TotalClaimed = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                }).OrderByDescending(x => x.Count).ToList();

            var monthlyTrends = claims
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new MonthlyTrend
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Claims = g.Count(),
                    Amount = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                }).OrderByDescending(x => x.Month).Take(12).ToList();

            var topFarms = claims
                .GroupBy(c => new
                {
                    c.FarmId,
                    FarmName = c.Farm!.Name,
                    FarmerName = c.Farm.User != null
                        ? c.Farm.User.FirstName + " " + c.Farm.User.LastName
                        : "Unknown"
                })
                .Select(g => new TopFarmDto
                {
                    FarmId = g.Key.FarmId,
                    FarmName = g.Key.FarmName,
                    FarmerName = g.Key.FarmerName,
                    ClaimCount = g.Count(),
                    TotalClaimed = g.Where(c => c.ApprovedAmount.HasValue).Sum(c => c.ApprovedAmount!.Value)
                }).OrderByDescending(x => x.ClaimCount).Take(5).ToList();

            double avgDays = 0;
            if (approved.Count > 0)
            {
                var reviewed = approved.Where(c => c.ReviewedAt.HasValue).ToList();
                if (reviewed.Count > 0)
                    avgDays = reviewed.Average(c => (c.ReviewedAt!.Value - c.CreatedAt).TotalDays);
            }

            return new DashboardStatsDto
            {
                // Claim stats (existing)
                TotalClaims = claims.Count,
                PendingClaims = pending.Count,
                ApprovedClaims = approved.Count,
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                UnderReviewClaims = claims.Count(c => c.Status == ClaimStatus.UnderReview),
                PaidClaims = claims.Count(c => c.Status == ClaimStatus.Paid),
                TotalPayoutAmount = approved.Sum(c => c.ApprovedAmount ?? 0),
                PendingPayoutAmount = pending
                    .Where(c => c.Policy != null)
                    .Sum(c => c.Policy!.SumInsured),
                ClaimsWithImages = claims.Count(c => c.Images.Any(i => !i.IsDeleted)),
                ClaimsWithAIAnalysis = claims.Count(c => c.AIAnalysisResult != null),
                ClaimsWithWeatherData = claims.Count(c => c.WeatherSnapshot != null),
                AverageProcessingDays = (decimal)Math.Round(avgDays, 1),

                // NEW: Policy stats
                TotalPolicies = policies.Count,
                PendingPolicies = policies.Count(p => p.Status == PolicyStatus.Pending),
                ActivePolicies = policies.Count(p => p.Status == PolicyStatus.Active),
                RejectedPolicies = policies.Count(p => p.Status == PolicyStatus.Rejected),
                ExpiredPolicies = policies.Count(p => p.Status == PolicyStatus.Expired),

                // NEW: User & Farm stats
                TotalFarmers = totalFarmers,
                TotalFarms = totalFarms,

                // Existing
                IncidentBreakdown = incidentBreakdown,
                MonthlyTrends = monthlyTrends,
                TopFarms = topFarms
            };
        }
    }
}