using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

namespace ShiftSoftware.ADP.Surveys.Shared.Json;

/// <summary>
/// Dispatches between <see cref="InlineScreenDto"/> and <see cref="ScreenTemplateRefDto"/>
/// by peeking for a <c>templateRef</c> property on the incoming JSON object.
/// </summary>
public class ScreenDtoConverter : JsonConverter<ScreenDto>
{
    public override ScreenDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object for ScreenDto.");

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var isRef = root.TryGetProperty("templateRef", out _);
        var targetType = isRef ? typeof(ScreenTemplateRefDto) : typeof(InlineScreenDto);

        return (ScreenDto)root.Deserialize(targetType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, ScreenDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
