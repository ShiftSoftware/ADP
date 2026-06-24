using System.Globalization;
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
            ReadInto(file, results);
        return results;
    }

    /// <summary>
    /// Like <see cref="ReadAll"/> but only the runs on or after <paramref name="since"/>. Uses the
    /// <c>date=&lt;yyyy-MM-dd&gt;</c> partition directories to skip older days without opening their files —
    /// the trends dashboard reads a bounded window every render, so it must not pay for all of history as it
    /// grows. The partition is whole-day granular, so a final <see cref="CheckResult.StartedAtUtc"/> filter
    /// trims the boundary day precisely.
    /// </summary>
    public IReadOnlyList<CheckResult> ReadSince(DateTimeOffset since)
    {
        var dir = Path.Combine(root, "results");
        if (!Directory.Exists(dir))
            return [];

        var sinceDate = since.UtcDateTime.Date;
        var results = new List<CheckResult>();
        foreach (var dateDir in Directory.EnumerateDirectories(dir, "date=*", SearchOption.AllDirectories))
        {
            // Skip whole days strictly before the window. An unparseable dir name (shouldn't happen) is kept,
            // not silently dropped — the StartedAtUtc filter below still bounds it correctly.
            if (TryParsePartitionDate(Path.GetFileName(dateDir), out var date) && date < sinceDate)
                continue;
            foreach (var file in Directory.EnumerateFiles(dateDir, "*.jsonl"))
                ReadInto(file, results);
        }
        return results.Where(r => r.StartedAtUtc >= since).ToList();
    }

    private void ReadInto(string file, List<CheckResult> results)
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

    private static bool TryParsePartitionDate(string dirName, out DateTime date)
    {
        const string prefix = "date=";
        date = default;
        return dirName.StartsWith(prefix, StringComparison.Ordinal)
            && DateTime.TryParseExact(dirName[prefix.Length..], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
