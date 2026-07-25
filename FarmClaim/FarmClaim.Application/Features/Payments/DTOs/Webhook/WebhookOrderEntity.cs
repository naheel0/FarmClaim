using System.Text.Json.Serialization;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class WebhookOrderEntity
    {
        [JsonPropertyName("entity")]
        public string Entity { get; set; } = "order";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("amount_paid")]
        public long AmountPaid { get; set; }

        [JsonPropertyName("amount_due")]
        public long AmountDue { get; set; }
    }
}