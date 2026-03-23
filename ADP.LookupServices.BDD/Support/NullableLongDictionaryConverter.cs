using System.Text.Json;
using System.Text.Json.Serialization;

namespace LookupServices.BDD.Support;

public class NullableLongDictionaryConverter : JsonConverter<Dictionary<long?, int>>
{
    public override Dictionary<long?, int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = new Dictionary<long?, int>();

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dict;

            var key = reader.GetString();
            reader.Read();
            var value = reader.GetInt32();

            long? parsedKey = long.TryParse(key, out var k) ? k : null;
            dict[parsedKey] = value;
        }

        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<long?, int> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key?.ToString() ?? "null");
            writer.WriteNumberValue(kvp.Value);
        }

        writer.WriteEndObject();
    }
}
