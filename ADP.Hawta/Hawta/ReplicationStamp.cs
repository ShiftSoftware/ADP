using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The <c>_ReplicationStamp</c> payload: the Cosmos coordinates actually written last, per
/// family (adopted from ShiftEntity's <c>LastReplicationStamp</c>, extended to fan-out).
/// Deletes and coordinate changes are driven from the OLD stamp — never recomputed from
/// current row values, which may already map elsewhere. An empty families map is the
/// "replication-excluded" marker: this row version required no Cosmos action, and that
/// action is complete.
/// </summary>
public sealed class ReplicationStamp
{
    [JsonPropertyName("families")]
    public Dictionary<string, StampCoordinates> Families { get; init; } = [];

    public sealed class StampCoordinates
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("pk")]
        public required List<JsonElement> PartitionKey { get; init; }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public static ReplicationStamp Parse(string? json) =>
        string.IsNullOrEmpty(json)
            ? new ReplicationStamp()
            : JsonSerializer.Deserialize<ReplicationStamp>(json, SerializerOptions) ?? new ReplicationStamp();

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static StampCoordinates ToCoordinates(CosmosDocument document) => new()
    {
        Id = document.Id,
        PartitionKey = document.PartitionKey
            .Select(v => JsonSerializer.SerializeToElement(v))
            .ToList(),
    };

    public static bool CoordinatesEqual(StampCoordinates a, CosmosDocument b) =>
        a.Id == b.Id
        && a.PartitionKey.Count == b.PartitionKey.Count
        && a.PartitionKey.Zip(b.PartitionKey).All(pair =>
            pair.First.GetRawText() == JsonSerializer.SerializeToElement(pair.Second).GetRawText());
}
