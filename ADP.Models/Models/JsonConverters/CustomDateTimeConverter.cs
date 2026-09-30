using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.JsonConverters
{
    /// <summary>
    /// Writes a date with a fixed format and reads it back. Both directions use the invariant
    /// culture, so the JSON does not depend on the culture of the thread. The server that writes a
    /// date and the server that reads it can run under different cultures: a lookup host writes a
    /// service item, and the claim endpoint reads it back and checks its signature. With the current
    /// culture, a culture whose calendar is not Gregorian wrote a year such as 2569, and read
    /// "2026-09-30" as the year 1483 or could not read it at all.
    /// </summary>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format;

        public CustomDateTimeConverter(string format)
        {
            _format = format;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The nullable form of <see cref="CustomDateTimeConverter"/>, with the same invariant culture.
    /// </summary>
    public class CustomDateTimeNullableConverter : JsonConverter<DateTime?>
    {
        private readonly string _format;

        public CustomDateTimeNullableConverter(string format)
        {
            _format = format;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString(_format, CultureInfo.InvariantCulture) ?? null);
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
        }
    }
}
