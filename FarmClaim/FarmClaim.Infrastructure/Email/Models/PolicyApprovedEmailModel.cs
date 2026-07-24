namespace FarmClaim.Infrastructure.Email.Models
{
    public class PolicyApprovedEmailModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string CropType { get; set; } = string.Empty;
        public decimal CoverageAmount { get; set; }
        public decimal SumInsured { get; set; }
        public DateTime EndDate { get; set; }
        public string DashboardUrl { get; set; } = string.Empty;
    }
}