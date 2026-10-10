using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// The canonical <c>events</c> artifact — one per golden, staged in <c>ProjectionState</c> — and the
/// single definition of its shape: the resolve writes it here and every reader parses it here, so
/// the writer and the serving API cannot drift apart.
///
/// <code>
/// {"goldenId":N,"events":[{"type":"ServiceVisit","id":"…","date":"yyyy-MM-dd","vin":"…",
///   "link":"Profile","src":"…","rid":"…","dealerId":"…","dealer":"…","title":"…","facts":{"k":"v"}}]}
/// </code>
///
/// Canonical: events in <see cref="Compare"/> order (newest first), facts by key (ordinal), null
/// members omitted, one fixed encoder — so identical attachment state serializes byte-identically
/// and the resolve's content hash is a faithful change detector (re-runs stay zero-delta).
/// </summary>
public static class GoldenEventsPayload
{
    /// <summary>The <c>ProjectionState.ArtifactType</c> of the family. Key = the golden id.</summary>
    public const string ArtifactType = "events";

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        // Relaxed: Cyrillic and other non-ASCII text stays readable in the staged payload. Quotes,
        // backslashes and control characters are still escaped, so the output is always valid JSON.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    /// <summary>
    /// Timeline order: date descending with undated events last, then type, then id (ordinal).
    /// Total and deterministic, which the hash discipline needs.
    /// </summary>
    public static int Compare(DateOnly? dateA, GoldenEventType typeA, string idA, DateOnly? dateB, GoldenEventType typeB, string idB)
    {
        if (dateA != dateB)
        {
            if (dateA is null) return 1;
            if (dateB is null) return -1;
            return dateB.Value.CompareTo(dateA.Value);
        }
        int byType = typeA.CompareTo(typeB);
        return byType != 0 ? byType : string.CompareOrdinal(idA, idB);
    }

    /// <summary>Serialize one golden's attached events canonically.</summary>
    public static string Write(long goldenId, IEnumerable<AttachedEvent> events)
    {
        var ordered = events.ToList();
        ordered.Sort((a, b) =>
        {
            int c = Compare(a.Event.Date, a.Event.Type, a.Event.EventId, b.Event.Date, b.Event.Type, b.Event.EventId);
            return c != 0 ? c : a.Link.CompareTo(b.Link);
        });

        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, WriterOptions))
        {
            w.WriteStartObject();
            w.WriteNumber("goldenId", goldenId);
            w.WriteStartArray("events");
            foreach (var (e, link) in ordered)
            {
                w.WriteStartObject();
                w.WriteString("type", e.Type.ToString());
                w.WriteString("id", e.EventId);
                if (e.Date is { } d) w.WriteString("date", d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(e.Vin)) w.WriteString("vin", e.Vin);
                w.WriteString("link", link.ToString());
                if (e.IsProfileLinked)
                {
                    w.WriteString("src", e.SourceSystem);
                    w.WriteString("rid", e.SourceRecordId);
                }
                if (!string.IsNullOrEmpty(e.DealerId)) w.WriteString("dealerId", e.DealerId);
                if (!string.IsNullOrEmpty(e.DealerName)) w.WriteString("dealer", e.DealerName);
                w.WriteString("title", e.Title ?? "");
                if (e.Facts is { Count: > 0 } facts)
                {
                    w.WriteStartObject("facts");
                    foreach (var kv in facts.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        w.WriteString(kv.Key, kv.Value ?? "");
                    w.WriteEndObject();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>One event as staged — what a reader gets back from <see cref="Parse"/>.</summary>
    public sealed record StagedEvent(
        GoldenEventType Type,
        string Id,
        DateOnly? Date,
        string? Vin,
        GoldenEventLink Link,
        string? SourceSystem,
        string? SourceRecordId,
        string? DealerId,
        string? DealerName,
        string Title,
        IReadOnlyDictionary<string, string> Facts);

    /// <summary>
    /// Parse a staged payload. Events of a type this build does not know are skipped rather than
    /// failing the whole read (forward-compatible, like the golden mapper's unknown attrs). The list
    /// is returned in staged order, which <see cref="Write"/> makes the <see cref="Compare"/> order.
    /// </summary>
    public static (long GoldenId, List<StagedEvent> Events) Parse(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        long goldenId = root.TryGetProperty("goldenId", out var g) && g.ValueKind == JsonValueKind.Number ? g.GetInt64() : 0;
        var list = new List<StagedEvent>();
        if (!root.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return (goldenId, list);

        foreach (var e in events.EnumerateArray())
        {
            if (!Enum.TryParse<GoldenEventType>(Str(e, "type"), ignoreCase: false, out var type)) continue;
            Enum.TryParse<GoldenEventLink>(Str(e, "link"), ignoreCase: false, out var link);
            DateOnly? date = Str(e, "date") is { Length: > 0 } ds
                && DateOnly.TryParseExact(ds, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed : null;
            var facts = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (e.TryGetProperty("facts", out var f) && f.ValueKind == JsonValueKind.Object)
                foreach (var p in f.EnumerateObject())
                    facts[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? "" : p.Value.GetRawText();
            list.Add(new StagedEvent(type, Str(e, "id") ?? "", date, Str(e, "vin"), link,
                Str(e, "src"), Str(e, "rid"), Str(e, "dealerId"), Str(e, "dealer"), Str(e, "title") ?? "", facts));
        }
        return (goldenId, list);
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
