using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// Batch of candidate events being submitted to <c>/api/triggers/ingest</c>.
/// All items in a batch share an <see cref="EventKind"/>; they're matched
/// against published triggers carrying that same kind.
/// </summary>
public class TriggerIngestRequest
{
    [JsonPropertyName("eventKind")]
    public string EventKind { get; set; } = "";

    [JsonPropertyName("items")]
    public List<TriggerCandidate> Items { get; set; } = new();
}

/// <summary>
/// One candidate event paired with the recipient it was resolved for. The
/// caller is responsible for recipient resolution upstream — by the time the
/// event reaches us, the phone/email/locale is already known.
/// </summary>
public class TriggerCandidate
{
    /// <summary>
    /// Free-form payload; the trigger's filter expressions and dedup recipe
    /// reference fields under <c>candidate.X</c>. Top-level properties are
    /// addressable; nested-path resolution lands when a trigger needs it.
    /// </summary>
    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }

    [JsonPropertyName("recipient")]
    public TriggerRecipient Recipient { get; set; } = new();
}

public class TriggerRecipient
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("customerRef")]
    public string? CustomerRef { get; set; }
}

/// <summary>
/// Per-batch summary returned from the ingest endpoint. <see cref="Items"/> is
/// 1:1 with the request items so callers can correlate by index.
/// </summary>
public class TriggerIngestResult
{
    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("items")]
    public List<TriggerIngestItemResult> Items { get; set; } = new();
}

public class TriggerIngestItemResult
{
    [JsonPropertyName("outcome")]
    public TriggerIngestOutcome Outcome { get; set; }

    /// <summary>Surveys that matched and produced an instance. Empty when no surveys matched or the item failed.</summary>
    [JsonPropertyName("instances")]
    public List<TriggerIngestInstanceRef> Instances { get; set; } = new();

    /// <summary>Populated when <see cref="Outcome"/> is <c>Failed</c>; explains the failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class TriggerIngestInstanceRef
{
    [JsonPropertyName("surveyId")]
    public long SurveyId { get; set; }

    [JsonPropertyName("triggerId")]
    public string TriggerId { get; set; } = "";

    [JsonPropertyName("publicId")]
    public Guid PublicId { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerIngestOutcome
{
    /// <summary>At least one matching trigger produced an instance for this item.</summary>
    Created,
    /// <summary>No trigger matched (no published survey carries this eventKind, or filter rejected the candidate).</summary>
    NoMatch,
    /// <summary>Match found but already-existing instance returned (slice 3 dedup path).</summary>
    Skipped,
    /// <summary>An exception happened processing this item; <see cref="TriggerIngestItemResult.Error"/> has details.</summary>
    Failed,
}
