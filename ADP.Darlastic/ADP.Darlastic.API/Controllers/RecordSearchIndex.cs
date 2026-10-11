using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Every staged record's names, phones, key and record index held in memory, one snapshot per registry and
/// resolve run (<see cref="Extensions.DarlasticApiOptions.RecordSearchIndex"/>, kept by
/// <see cref="Extensions.RegistryRunCache"/>), so the case browser finds a staged record by name, phone, key or
/// index without unpacking every staged payload.
///
/// <para><b>Why.</b> A staged record's name and phones live inside its JSON payload. Searching them in SQL
/// unpacks every payload on every search — fine at tens of thousands of staged records, ten seconds a
/// search (and timeouts under concurrent use) at well over a million. They change only when a resolve
/// finishes, so they are read once per run (one pass over the table) and kept.</para>
///
/// <para><b>Candidates, not answers.</b> A search here returns record positions in key order whose stored
/// text contains the search text (or equals it, value by value), ignoring case. The caller then reads those
/// rows and keeps the ones its own rule matches, so the index decides nothing the rule would not: it only
/// spares the rule from reading every row.</para>
///
/// <para><b>Layout.</b> Each column is one UTF-8 buffer of upper-cased values, each value followed by a zero
/// byte (the buffer starts with one too), plus the offset where each record's values start. A record's
/// values are contiguous and in key order, so a scan of the buffer meets records in key order; a whole
/// value is the search text with a zero byte on both sides. About seventy bytes per staged record.</para>
/// </summary>
internal sealed class RecordSearchIndex
{
    /// <summary>The cache entry the snapshot is kept under.</summary>
    public const string CacheName = "case-browser-record-search";

    /// <summary>The value columns a search can look in.</summary>
    [Flags]
    public enum Fields
    {
        None = 0,
        /// <summary>The record's name, as written and as normalized.</summary>
        Names = 1,
        Phones = 2,
        WeakPhones = 4,
        /// <summary>The record's ID within its source.</summary>
        RecordIds = 8,
    }

    private const byte Sep = 0;

    /// <summary>One column: its buffer and where each record's values start (one entry per record plus the end).</summary>
    private sealed class Column
    {
        public required byte[] Text { get; init; }
        public required int[] Start { get; init; }
    }

    private readonly string[] systems;
    private readonly int[] systemOf;
    private readonly Column keys;
    private readonly Column ids;
    private readonly Column names;
    private readonly Column phones;
    private readonly Column weakPhones;
    private readonly int[] idxSorted;
    private readonly int[] idxPosition;

    /// <summary>Staged records in the snapshot.</summary>
    public int Count => systemOf.Length;

    /// <summary>Bytes the snapshot holds, for the build log.</summary>
    public long Bytes =>
        (long)systemOf.Length * 4 + idxSorted.Length * 8L
        + new[] { keys, ids, names, phones, weakPhones }.Sum(c => c.Text.Length + c.Start.Length * 4L);

    private RecordSearchIndex(string[] systems, int[] systemOf, Column keys, Column ids, Column names, Column phones,
        Column weakPhones, int[] idxSorted, int[] idxPosition)
    {
        this.systems = systems;
        this.systemOf = systemOf;
        this.keys = keys;
        this.ids = ids;
        this.names = names;
        this.phones = phones;
        this.weakPhones = weakPhones;
        this.idxSorted = idxSorted;
        this.idxPosition = idxPosition;
    }

    /// <summary>One staged record as the index reads it.</summary>
    internal readonly record struct Row(string SourceSystem, string SourceRecordId, int? Idx, string? NormName,
        string? RawName, IReadOnlyList<string>? Phones, IReadOnlyList<string>? WeakPhones);

    /// <summary>Builds the snapshot from rows given in key order (source, then record ID).</summary>
    internal static RecordSearchIndex Build(IEnumerable<Row> rows)
    {
        var builder = new Builder();
        foreach (var r in rows) builder.Add(r);
        return builder.Finish();
    }

    /// <summary>Reads the snapshot of the registry <paramref name="db"/> reads: one pass over the staged records,
    /// reading only the values a search looks at (the payloads are unpacked by SQL, not shipped).</summary>
    public static async Task<RecordSearchIndex> BuildAsync(ShiftDbContext db, CancellationToken ct)
    {
        var entity = db.Model.FindEntityType(typeof(StewardRecord))
            ?? throw new InvalidOperationException("The registry model has no staged records.");
        string table = Quote(entity.GetSchema()) is { } schema
            ? $"{schema}.{Quote(entity.GetTableName())}"
            : Quote(entity.GetTableName())!;

        var connection = db.Database.GetDbConnection();
        bool opened = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            opened = true;
        }
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT SourceSystem, SourceRecordId, JSON_VALUE(Payload, '$.Idx'),
                       JSON_VALUE(Payload, '$.NormName'), JSON_VALUE(Payload, '$.RawName'),
                       JSON_QUERY(Payload, '$.Phones'), JSON_QUERY(Payload, '$.WeakPhones')
                FROM {table}
                ORDER BY SourceSystem, SourceRecordId
                """;
            // One scan of every staged payload: tens of seconds on a busy server at millions of records. It runs
            // once per resolve run, and every search waits for it rather than scanning on its own.
            command.CommandTimeout = 900;
            if (db.Database.CurrentTransaction is { } tx) command.Transaction = tx.GetDbTransaction();

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
            var builder = new Builder();
            while (await reader.ReadAsync(ct))
            {
                string system = reader.GetString(0), id = reader.GetString(1);
                string? idx = reader.IsDBNull(2) ? null : reader.GetString(2);
                string? norm = reader.IsDBNull(3) ? null : reader.GetString(3);
                string? raw = reader.IsDBNull(4) ? null : reader.GetString(4);
                string? ph = reader.IsDBNull(5) ? null : reader.GetString(5);
                string? weak = reader.IsDBNull(6) ? null : reader.GetString(6);
                builder.Add(new Row(system, id, int.TryParse(idx, out int i) ? i : null, norm, raw, Strings(ph), Strings(weak)));
            }
            return builder.Finish();
        }
        finally
        {
            if (opened) await connection.CloseAsync();
        }
    }

    /// <summary>
    /// Record positions, in key order and each once, that answer any of <paramref name="searches"/>: a value in one of
    /// its <c>Whole</c> columns equal to its text, or a value in one of its <c>Contains</c> columns containing it. Case
    /// is ignored (both sides upper-cased); an empty text finds nothing.
    /// </summary>
    public IEnumerable<int> Find(params (Fields Whole, Fields Contains, string? Text)[] searches)
    {
        var sources = new List<IEnumerator<int>>();
        foreach (var (whole, contains, text) in searches)
        {
            if (Normal(text) is not { } value) continue;
            var needle = Encoding.UTF8.GetBytes(value.ToUpperInvariant());
            foreach (var (field, column) in Columns())
            {
                if ((whole & field) != 0) sources.Add(Scan(column, needle, wholeValue: true).GetEnumerator());
                if ((contains & field) != 0) sources.Add(Scan(column, needle, wholeValue: false).GetEnumerator());
            }
        }
        return Merge(sources);
    }

    /// <summary>The position of the staged record carrying the engine's record index <paramref name="idx"/>, or -1.</summary>
    public int PositionOfIdx(int idx)
    {
        int at = Array.BinarySearch(idxSorted, idx);
        if (at < 0) return -1;
        while (at > 0 && idxSorted[at - 1] == idx) at--;   // a repeated index answers with its first record in key order
        return idxPosition[at];
    }

    /// <summary>The staged record at <paramref name="position"/>.</summary>
    public (string SourceSystem, string SourceRecordId) KeyAt(int position)
    {
        int start = keys.Start[position], end = keys.Start[position + 1];
        return (systems[systemOf[position]], Encoding.UTF8.GetString(keys.Text, start, end - start - 1));
    }

    /// <summary>
    /// The staged rows at <paramref name="positions"/> (in that order) that <paramref name="keep"/> accepts, until
    /// <paramref name="wanted"/> are found or <paramref name="maxExamined"/> positions were read. Rows are read by key,
    /// a page at a time.
    /// </summary>
    public async Task<List<StewardRecord>> RowsAsync(ShiftDbContext db, IEnumerable<int> positions, int wanted,
        Func<StewardRecord, bool> keep, CancellationToken ct, int maxExamined = 5_000)
    {
        var hits = new List<StewardRecord>();
        using var e = positions.GetEnumerator();
        int examined = 0;
        int page = Math.Clamp(wanted * 2, 50, 500);
        var chunk = new List<(string Sys, string Id)>(page);
        while (hits.Count < wanted && examined < maxExamined)
        {
            chunk.Clear();
            while (chunk.Count < page && examined < maxExamined && e.MoveNext())
            {
                chunk.Add(KeyAt(e.Current));
                examined++;
            }
            if (chunk.Count == 0) break;

            // EF cannot translate a tuple-set contains: one key seek list per source (positions in key order keep a
            // page to one or two sources; short record IDs repeat across sources, so a single ID list would not do).
            var rows = new Dictionary<(string, string), StewardRecord>();
            foreach (var group in chunk.GroupBy(k => k.Sys))
            {
                string system = group.Key;
                var idList = group.Select(k => k.Id).Distinct().ToList();
                foreach (var row in await db.Set<StewardRecord>().AsNoTracking()
                             .Where(r => r.SourceSystem == system && idList.Contains(r.SourceRecordId))
                             .ToListAsync(ct))
                    rows[(row.SourceSystem, row.SourceRecordId)] = row;
            }
            foreach (var key in chunk)
            {
                if (!rows.TryGetValue(key, out var row) || !keep(row)) continue;
                hits.Add(row);
                if (hits.Count == wanted) break;
            }
        }
        return hits;
    }

    private IEnumerable<(Fields Field, Column Column)> Columns()
    {
        yield return (Fields.Names, names);
        yield return (Fields.Phones, phones);
        yield return (Fields.WeakPhones, weakPhones);
        yield return (Fields.RecordIds, ids);
    }

    /// <summary>The positions of the records whose values in <paramref name="column"/> hold the needle, in order.</summary>
    private IEnumerable<int> Scan(Column column, byte[] needle, bool wholeValue)
    {
        byte[] pattern = needle;
        if (wholeValue)
        {
            pattern = new byte[needle.Length + 2];
            needle.CopyTo(pattern, 1);
        }
        int from = 0;
        while (from < column.Text.Length)
        {
            int hit = column.Text.AsSpan(from).IndexOf(pattern);
            if (hit < 0) yield break;
            // A whole value's match starts at the zero byte before it; the value itself starts one byte later.
            int at = from + hit + (wholeValue ? 1 : 0);
            int position = PositionAt(column, at);
            yield return position;
            // On to the next record. A whole value needs the zero byte that ends this record's last value.
            from = column.Start[position + 1] - (wholeValue ? 1 : 0);
        }
    }

    /// <summary>The record whose values span <paramref name="offset"/> (the last start at or before it).</summary>
    private static int PositionAt(Column column, int offset)
    {
        int lo = 0, hi = column.Start.Length - 1;   // first index with Start > offset lies in (lo, hi]
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (column.Start[mid] > offset) hi = mid; else lo = mid + 1;
        }
        return lo - 1;
    }

    /// <summary>Merges ascending position streams into one ascending stream without repeats.</summary>
    private static IEnumerable<int> Merge(List<IEnumerator<int>> sources)
    {
        var live = sources.Where(s => s.MoveNext()).ToList();
        try
        {
            int last = -1;
            while (live.Count > 0)
            {
                int min = 0;
                for (int i = 1; i < live.Count; i++)
                    if (live[i].Current < live[min].Current) min = i;
                int next = live[min].Current;
                if (next != last) yield return next;
                last = next;
                if (!live[min].MoveNext())
                {
                    live[min].Dispose();
                    live.RemoveAt(min);
                }
            }
        }
        finally
        {
            foreach (var s in live) s.Dispose();
        }
    }

    /// <summary>A value as the index holds it: trailing spaces dropped (SQL compares that way), zero bytes made
    /// spaces (they separate values), and null when nothing is left.</summary>
    private static string? Normal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var v = value.Replace('\0', ' ').TrimEnd();
        return v.Length == 0 ? null : v;
    }

    private static string[]? Strings(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<string[]>(json); }
        catch (JsonException) { return null; }   // a malformed array costs that record's phones, not the index
    }

    private static string? Quote(string? identifier) =>
        string.IsNullOrEmpty(identifier) ? null : "[" + identifier.Replace("]", "]]") + "]";

    /// <summary>Takes rows in key order, one at a time, so the build never holds the rows themselves.</summary>
    private sealed class Builder
    {
        private readonly Dictionary<string, int> systemIndex = new(StringComparer.Ordinal);
        private readonly List<string> systemList = [];
        private readonly List<int> systemOf = [];
        private readonly List<(int Idx, int Position)> idxOf = [];
        private readonly ColumnWriter keys = new(), ids = new(), names = new(), phones = new(), weakPhones = new();

        public void Add(Row r)
        {
            if (!systemIndex.TryGetValue(r.SourceSystem, out int s))
            {
                s = systemList.Count;
                systemIndex[r.SourceSystem] = s;
                systemList.Add(r.SourceSystem);
            }
            int position = systemOf.Count;
            systemOf.Add(s);
            if (r.Idx is { } idx) idxOf.Add((idx, position));

            // The key as stored (to read the row back); every searched column upper-cased, as the search text is.
            keys.StartRecord();
            keys.Add(r.SourceRecordId);

            ids.StartRecord();
            if (Normal(r.SourceRecordId) is { } id) ids.Add(id.ToUpperInvariant());

            names.StartRecord();
            string? norm = Normal(r.NormName)?.ToUpperInvariant(), raw = Normal(r.RawName)?.ToUpperInvariant();
            if (norm is not null) names.Add(norm);
            if (raw is not null && raw != norm) names.Add(raw);

            phones.StartRecord();
            foreach (var p in r.Phones ?? []) if (Normal(p) is { } v) phones.Add(v.ToUpperInvariant());

            weakPhones.StartRecord();
            foreach (var p in r.WeakPhones ?? []) if (Normal(p) is { } v) weakPhones.Add(v.ToUpperInvariant());
        }

        public RecordSearchIndex Finish()
        {
            var byIdx = idxOf.OrderBy(x => x.Idx).ThenBy(x => x.Position).ToArray();
            return new RecordSearchIndex([.. systemList], [.. systemOf], keys.Finish(), ids.Finish(), names.Finish(),
                phones.Finish(), weakPhones.Finish(), [.. byIdx.Select(x => x.Idx)], [.. byIdx.Select(x => x.Position)]);
        }
    }

    private sealed class ColumnWriter
    {
        private readonly MemoryStream text = new();
        private readonly List<int> start = [];

        public ColumnWriter() => text.WriteByte(Sep);

        public void StartRecord() => start.Add((int)text.Length);

        public void Add(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            text.Write(bytes);
            text.WriteByte(Sep);
        }

        public Column Finish()
        {
            start.Add((int)text.Length);
            return new Column { Text = text.ToArray(), Start = [.. start] };
        }
    }
}
