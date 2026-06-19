using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Append-only, partitioned result sink: one file per (domain, date, run) under
/// <c>{root}/results/domain=&lt;d&gt;/date=&lt;yyyy-MM-dd&gt;/&lt;runId&gt;.jsonl</c>. Each writer owns its own
/// files, so multiple apps can write to the same share with no lock contention; the dashboard unions
/// them at read time. (Parquet can replace JSONL later without changing the layout.)
/// </summary>
public sealed class JsonlResultSink(string root)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task WriteAsync(IEnumerable<CheckResult> results, string runId, DateTimeOffset runDateUtc, CancellationToken ct = default)
    {
        foreach (var byDomain in results.GroupBy(r => string.IsNullOrWhiteSpace(r.Domain) ? "unknown" : r.Domain))
        {
            var dir = Path.Combine(root, "results", $"domain={byDomain.Key}", $"date={runDateUtc:yyyy-MM-dd}");
            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, $"{runId}.jsonl");
            var lines = byDomain.Select(r => JsonSerializer.Serialize(r, Json));
            await File.WriteAllLinesAsync(file, lines, ct);
        }
    }

    public IReadOnlyList<CheckResult> ReadAll()
    {
        var dir = Path.Combine(root, "results");
        if (!Directory.Exists(dir))
            return [];

        var results = new List<CheckResult>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.jsonl", SearchOption.AllDirectories))
        {
            foreach (var line in File.ReadLines(file))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var parsed = JsonSerializer.Deserialize<CheckResult>(line, Json);
                if (parsed is not null)
                    results.Add(parsed);
            }
        }
        return results;
    }
}
