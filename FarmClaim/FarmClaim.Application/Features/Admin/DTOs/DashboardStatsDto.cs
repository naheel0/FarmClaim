using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record DashboardStatsDto
    {
        // Claim stats (existing — unchanged)
        public int TotalClaims { get; init; }
        public int PendingClaims { get; init; }
        public int ApprovedClaims { get; init; }
        public int RejectedClaims { get; init; }
        public int UnderReviewClaims { get; init; }
        public int PaidClaims { get; init; }
        public decimal TotalPayoutAmount { get; init; }
        public decimal PendingPayoutAmount { get; init; }
        public int ClaimsWithImages { get; init; }
        public int ClaimsWithAIAnalysis { get; init; }
        public int ClaimsWithWeatherData { get; init; }
        public decimal AverageProcessingDays { get; init; }

        // NEW: Policy stats
        public int TotalPolicies { get; init; }
        public int PendingPolicies { get; init; }
        public int ActivePolicies { get; init; }
        public int RejectedPolicies { get; init; }
        public int ExpiredPolicies { get; init; }

        // NEW: User & Farm stats
        public int TotalFarmers { get; init; }
        public int TotalFarms { get; init; }

        // Existing (unchanged)
        public List<IncidentTypeBreakdown> IncidentBreakdown { get; init; } = new();
        public List<MonthlyTrend> MonthlyTrends { get; init; } = new();
        public List<TopFarmDto> TopFarms { get; init; } = new();
    }

    public record IncidentTypeBreakdown
    {
        public string IncidentType { get; init; } = string.Empty;
        public int Count { get; init; }
        public decimal TotalClaimed { get; init; }
    }

    public record MonthlyTrend
    {
        public string Month { get; init; } = string.Empty;
        public int Claims { get; init; }
        public decimal Amount { get; init; }
    }

    public record TopFarmDto
    {
        public Guid FarmId { get; init; }
        public string FarmName { get; init; } = string.Empty;
        public string FarmerName { get; init; } = string.Empty;
        public int ClaimCount { get; init; }
        public decimal TotalClaimed { get; init; }
    }
}