using System.Text;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Every golden's name and phone held in memory, one snapshot per registry and resolve run (part of
/// <see cref="Extensions.DarlasticApiOptions.GoldenRowIndex"/>, kept by <see cref="Extensions.RegistryRunCache"/>),
/// so the case browser's customer search does not unpack every golden's payload per search.
///
/// <para><b>Why.</b> A golden's name and phone live inside its payload. Searching them through the
/// <c>GoldenCustomer</c> view unpacks every payload on every search: seven to nine seconds at a million goldens.
/// They change only when a resolve finishes, so they are read once per run (one pass through the view) and kept;
/// a search is then a scan of two byte buffers. Built by the first search after a run, or ahead of it by
/// <see cref="Extensions.RegistryRunWarmup"/>.</para>
///
/// <para><b>The same rule.</b> A name matches when it holds the search text (a caller with the partial-search
/// action) or equals it (anyone else); a phone the same way on its digits. Case is ignored by upper-casing both
/// sides, and trailing spaces do not count, as in SQL; a database whose collation also ignores accents matched
/// more through the view than this does.</para>
///
/// <para><b>Layout.</b> Each column is one UTF-8 buffer of upper-cased values, one per golden in the order read,
/// each followed by a zero byte (the buffer starts with one too), plus the offset where each golden's value
/// starts. About forty bytes per golden.</para>
/// </summary>
internal sealed class GoldenSearchIndex
{
    /// <summary>The cache entry the snapshot is kept under.</summary>
    public const string CacheName = "case-browser-golden-search";

    private const byte Sep = 0;

    private readonly long[] ids;
    private readonly Column names;
    private readonly Column phones;

    private sealed record Column(byte[] Text, int[] Start);

    private GoldenSearchIndex(long[] ids, Column names, Column phones)
    {
        this.ids = ids;
        this.names = names;
        this.phones = phones;
    }

    /// <summary>Goldens in the snapshot.</summary>
    public int Count => ids.Length;

    /// <summary>Bytes the snapshot holds.</summary>
    public long Bytes => ids.Length * 8L + names.Text.Length + phones.Text.Length + (names.Start.Length + phones.Start.Length) * 4L;

    /// <summary>Builds the snapshot from every golden's ID, name and phone.</summary>
    internal static GoldenSearchIndex Build(IEnumerable<(long Id, string? FullName, string? Phone)> rows)
    {
        var builder = new Builder();
        foreach (var (id, name, phone) in rows) builder.Add(id, name, phone);
        return builder.Finish();
    }

    /// <summary>Reads the snapshot of the registry <paramref name="db"/> reads: one pass through the view, a row at
    /// a time.</summary>
    public static async Task<GoldenSearchIndex> BuildAsync(ShiftDbContext db, CancellationToken ct)
    {
        // One unpacking of every golden's payload: seconds here, longer on a small server. It runs once per run.
        bool relational = db.Database.IsRelational();
        int? timeout = relational ? db.Database.GetCommandTimeout() : null;
        if (relational) db.Database.SetCommandTimeout(900);
        try
        {
            var builder = new Builder();
            await foreach (var g in db.Set<GoldenCustomer>().AsNoTracking()
                               .Select(g => new { g.ID, g.FullName, g.Phone })
                               .AsAsyncEnumerable().WithCancellation(ct))
                builder.Add(g.ID, g.FullName, g.Phone);
            return builder.Finish();
        }
        finally
        {
            if (relational) db.Database.SetCommandTimeout(timeout);
        }
    }

    /// <summary>The goldens whose name holds <paramref name="needle"/> (<paramref name="partial"/>) or equals it.</summary>
    public HashSet<long> ByName(string needle, bool partial) => Find(names, needle, partial);

    /// <summary>The goldens whose phone holds <paramref name="digits"/> (<paramref name="partial"/>) or equals it.</summary>
    public HashSet<long> ByPhone(string digits, bool partial) => Find(phones, digits, partial);

    private HashSet<long> Find(Column column, string? text, bool partial)
    {
        var found = new HashSet<long>();
        if (Normal(text) is not { } value) return found;
        byte[] needle = Encoding.UTF8.GetBytes(value.ToUpperInvariant());
        byte[] pattern = needle;
        if (!partial)
        {
            // A whole value is the text with a zero byte on both sides.
            pattern = new byte[needle.Length + 2];
            needle.CopyTo(pattern, 1);
        }

        int from = 0;
        while (from < column.Text.Length)
        {
            int hit = column.Text.AsSpan(from).IndexOf(pattern);
            if (hit < 0) break;
            int at = from + hit + (partial ? 0 : 1);
            int position = PositionAt(column, at);
            found.Add(ids[position]);
            // On to the next golden. A whole value needs the zero byte that ends this golden's value.
            from = column.Start[position + 1] - (partial ? 0 : 1);
        }
        return found;
    }

    /// <summary>The golden whose value spans <paramref name="offset"/> (the last start at or before it).</summary>
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

    /// <summary>A value as the index holds it: trailing spaces dropped (SQL compares that way), zero bytes made
    /// spaces (they separate values), and null when nothing is left.</summary>
    private static string? Normal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var v = value.Replace('\0', ' ').TrimEnd();
        return v.Length == 0 ? null : v;
    }

    private sealed class Builder
    {
        private readonly List<long> ids = [];
        private readonly MemoryStream names = new(), phones = new();
        private readonly List<int> nameStart = [], phoneStart = [];

        public Builder()
        {
            names.WriteByte(Sep);
            phones.WriteByte(Sep);
        }

        public void Add(long id, string? name, string? phone)
        {
            ids.Add(id);
            Write(names, nameStart, name);
            Write(phones, phoneStart, phone);
        }

        private static void Write(MemoryStream text, List<int> start, string? value)
        {
            start.Add((int)text.Length);
            if (Normal(value) is { } v) text.Write(Encoding.UTF8.GetBytes(v.ToUpperInvariant()));
            text.WriteByte(Sep);
        }

        public GoldenSearchIndex Finish()
        {
            nameStart.Add((int)names.Length);
            phoneStart.Add((int)phones.Length);
            return new GoldenSearchIndex([.. ids], new Column(names.ToArray(), [.. nameStart]), new Column(phones.ToArray(), [.. phoneStart]));
        }
    }
}
