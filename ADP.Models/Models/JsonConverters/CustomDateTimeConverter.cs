using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.JsonConverters
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format;

        public CustomDateTimeConverter(string format)
        {
            _format = format;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }
    }

    public class CustomDateTimeNullableConverter : JsonConverter<DateTime?>
    {
        private readonly string _format;

        public CustomDateTimeNullableConverter(string format)
        {
            _format = format;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString(_format) ?? null);
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return DateTime.Parse(reader.GetString()!);
        }
    }
}
