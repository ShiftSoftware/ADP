using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

namespace ShiftSoftware.ADP.Surveys.Shared.Json;

/// <summary>
/// Reads the three condition shapes (<c>expression</c>, <c>all</c>/<c>any</c> composite,
/// <c>questionId</c> predicate) into a polymorphic <see cref="LogicConditionDto"/>.
/// </summary>
public class LogicConditionDtoConverter : JsonConverter<LogicConditionDto>
{
    public override LogicConditionDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object for LogicConditionDto.");

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("expression", out _))
            return root.Deserialize<ExpressionConditionDto>(options)!;

        if (root.TryGetProperty("all", out _) || root.TryGetProperty("any", out _))
            return root.Deserialize<CompositeConditionDto>(options)!;

        if (root.TryGetProperty("questionId", out _))
            return root.Deserialize<PredicateConditionDto>(options)!;

        throw new JsonException("LogicConditionDto JSON must contain 'expression', 'all'/'any', or 'questionId'.");
    }

    public override void Write(Utf8JsonWriter writer, LogicConditionDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
