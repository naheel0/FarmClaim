using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmClaim.API.Services
{
    // EF Core rehydrates DateTime columns with Kind=Unspecified, even though the
    // values are stored as UTC. System.Text.Json then serializes them without a
    // UTC marker, so clients parse the timestamp as local time and display wrong
    // relative times (e.g. "5h ago" for a claim filed minutes earlier).
    // This converter emits an explicit 'Z' (UTC) suffix for those values.
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.Kind == DateTimeKind.Unspecified)
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    public sealed class UtcDateTimeNullableJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            if (value.Value.Kind == DateTimeKind.Unspecified)
                value = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            writer.WriteStringValue(value.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}
