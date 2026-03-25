using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    // Definition of Log() as requested
    private void Log(string message) => Console.WriteLine($"[LOG]: {message}");

    public override bool CanConvert(Type typeToConvert)
    {
        return //typeToConvert == typeof(DateTimeOffset) ||
            typeToConvert == typeof(DateTimeOffset?);
    }

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Branch 1: The JSON token is a String
        if (reader.TokenType == JsonTokenType.String)
        {
            Log("Branch: Token is String.");
            var str = reader.GetString();

            // Branch 1.1: String is empty or whitespace
            if (string.IsNullOrWhiteSpace(str))
            {
                Log("Branch: String is empty/whitespace. Returning null.");
                return null;
            }

            // Branch 1.2: String is a valid DateTimeOffset format
            if (DateTimeOffset.TryParse(str, out var dto))
            {
                Log($"Branch: String parsed successfully. Value: {dto}");
                return dto;
            }

            // Branch 1.3: String is NOT empty, but format is invalid
            Log($"Branch: String '{str}' is invalid format. Returning null.");
            return null;
        }

        // Branch 2: The JSON token is explicitly Null
        if (reader.TokenType == JsonTokenType.Null)
        {
            Log("Branch: Token is explicit Null. Returning null.");
            return null;
        }

        // Branch 3: Default fallback for Numbers or Objects
        Log("Branch: Token is neither String nor Null (likely Number/Object). Attempting native read.");
        try
        {
            // Branch 3.1: Native read succeeds
            var result = reader.GetDateTimeOffset();
            Log($"Branch: Native read succeeded. Value: {result}");
            return result;
        }
        catch
        {
            // Branch 3.2: Native read fails (Exception caught)
            Log("Branch: Native read threw exception. Returning null.");
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        // Branch 4: Writing a non-null value
        if (value.HasValue)
        {
            Log($"Branch: Writing non-null value: {value.Value}");
            writer.WriteStringValue(value.Value);
        }
        // Branch 5: Writing a null value
        else
        {
            Log("Branch: Writing null value.");
            writer.WriteNullValue();
        }
    }
}