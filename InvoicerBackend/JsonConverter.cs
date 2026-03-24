using System.Text.Json;
using System.Text.Json.Serialization;

public class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If it's a string, try to parse it
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return null; // Empty string -> null

            if (DateTimeOffset.TryParse(str, out var dto))
                return dto;

            // Invalid format -> null (instead of throwing)
            return null;
        }

        // If it's explicitly null in JSON
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Default fallback for numbers/objects
        try
        {
            return reader.GetDateTimeOffset();
        }
        catch
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}