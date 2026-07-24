namespace FarmClaim.Infrastructure.Email.Models
{
    public class ClaimStatusChangedEmailModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public Guid ClaimId { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? Message { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string DashboardUrl { get; set; } = string.Empty;
    }
}