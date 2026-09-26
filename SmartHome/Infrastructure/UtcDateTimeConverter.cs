using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SmartHome.Infrastructure;

public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is null || !Regex.IsMatch(text, @"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            throw new JsonException("Укажите дату ISO 8601 с Z или явным смещением, например 2026-01-01T12:00:00+03:00.");
        return value.UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime());
}
