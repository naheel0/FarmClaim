using System.Text.Json.Serialization;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class RazorpayWebhookEventDto
    {
        [JsonPropertyName("entity")]
        public string Entity { get; set; } = "event";

        [JsonPropertyName("account_id")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("contains")]
        public List<string> Contains { get; set; } = new();

        [JsonPropertyName("payload")]
        public WebhookPayload Payload { get; set; } = new();

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
    }
}