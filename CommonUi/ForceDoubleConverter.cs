using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ForceDoubleConverter : JsonConverter<double>
{
    public override double Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.GetDouble();
    }

    public override void Write(
        Utf8JsonWriter writer,
        double value,
        JsonSerializerOptions options)
    {
        // Use the default general ("G") format for doubles.
        string defaultFormatted = value.ToString("G", CultureInfo.InvariantCulture);

        // If the formatted value does NOT include a dot (and is not in exponent notation),
        // then it's pretty much an integer. In that case, append ".0".
        string formatted;
        if (!defaultFormatted.Contains('.') &&
            !defaultFormatted.Contains('e') &&
            !defaultFormatted.Contains('E'))
        {
            formatted = defaultFormatted + ".0";
        }
        else
        {
            formatted = defaultFormatted;
        }

        // Write the numeric value as raw JSON.
        writer.WriteRawValue(formatted);
    }
}
