namespace FarmClaim.Infrastructure.Email.Models
{
    public class PolicyRejectedEmailModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string CropType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}