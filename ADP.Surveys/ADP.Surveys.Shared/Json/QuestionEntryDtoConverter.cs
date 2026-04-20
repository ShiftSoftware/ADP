using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

namespace ShiftSoftware.ADP.Surveys.Shared.Json;

/// <summary>
/// Reads either an inline <see cref="QuestionDto"/> (identified by a <c>type</c> property)
/// or a <see cref="QuestionRefDto"/> (identified by a <c>bankRef</c> property) from JSON,
/// into a single <see cref="QuestionEntryDto"/> slot.
/// </summary>
public class QuestionEntryDtoConverter : JsonConverter<QuestionEntryDto>
{
    public override QuestionEntryDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object for QuestionEntryDto.");

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("bankRef", out _))
        {
            var refDto = root.Deserialize<QuestionRefDto>(options)!;
            return QuestionEntryDto.FromRef(refDto);
        }

        var inline = root.Deserialize<QuestionDto>(options)!;
        return QuestionEntryDto.FromInline(inline);
    }

    public override void Write(Utf8JsonWriter writer, QuestionEntryDto value, JsonSerializerOptions options)
    {
        if (value.Ref is not null)
        {
            JsonSerializer.Serialize(writer, value.Ref, options);
            return;
        }

        if (value.Inline is not null)
        {
            // Must serialize through the base type so [JsonPolymorphic] on QuestionDto
            // emits the "type" discriminator. Writing via GetType() would skip it.
            JsonSerializer.Serialize(writer, value.Inline, typeof(QuestionDto), options);
            return;
        }

        throw new JsonException("QuestionEntryDto must carry either Inline or Ref.");
    }
}
