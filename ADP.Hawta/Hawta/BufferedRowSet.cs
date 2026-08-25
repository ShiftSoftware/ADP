using System.Collections;
using System.Data.Common;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A source result set drained into memory, so the FETCH that produced it can run off the
/// store's thread while the MERGE that consumes it stays serial.
///
/// <para><b>Why this type exists.</b> Measured across five prod-parity cycles, fetch is
/// 95.4–97.9 % of an ingest cycle — 99.8 % once the Cosmos drain is attributed correctly — and one
/// source spends 100 seconds fetching 7,700 rows and 12 ms merging them. The wait is a remote
/// server, so the only thing worth overlapping is the wait. Everything downstream of it
/// (staging, the in-DB hash, the merge) needs the store's single connection and stays where it
/// is.</para>
///
/// <para><b>The price, stated plainly.</b> <see cref="SqlViewSnapshotIngestor"/>'s reader-level
/// entry point resolves ordinals by name off a <see cref="DbDataReader"/>, so buffering means
/// standing a reader back up over a materialised set: one <c>object?[]</c> per row, and the cells
/// are boxed. That is real memory, which is why the dispatcher bounds it — see
/// <see cref="SnapshotIngestDispatcherOptions.MaxBufferedRows"/>. Against the measured estate
/// (~30 K rows across all app tables; 9,254 / 12,567 / 6,210 / 5,224 across the four DMS families
/// at three dealers) it is a bounded tens-of-MB, not an open-ended one.</para>
///
/// <para><b>Every column the reader returned is buffered</b>, not just the ones the table
/// declares. Narrowing here would move a missing-column failure from the merge's thread to the
/// worker's and change which run record it produces; the SELECTs already project narrowly, so the
/// saving would be nil and the divergence real.</para>
///
/// <para><b>A partial drain never becomes one of these.</b> <see cref="Drain"/> constructs the set
/// only on a clean exit — a cancelled or faulted read throws instead of returning a short set.
/// That is the structural answer to "nothing drops staging after a faulted drain": under
/// fetch-ahead nothing is staged at all until a COMPLETE buffer reaches the serial drain, so
/// there is no half-populated staging table to detect or sweep.</para>
/// </summary>
public sealed class BufferedRowSet
{
    /// <summary>
    /// Rows between progress callbacks. Reporting mid-fetch is what lets the admission window see
    /// a backlog growing before the source that owns it finishes; per-row would be an interlocked
    /// add per row for no extra fidelity at these sizes.
    /// </summary>
    private const int ProgressInterval = 1024;

    private readonly string[] names;
    private readonly Type[] types;
    private readonly Dictionary<string, int> exactOrdinals;
    private readonly Dictionary<string, int> looseOrdinals;
    private readonly List<object?[]> rows;

    private BufferedRowSet(string[] names, Type[] types, List<object?[]> rows)
    {
        this.names = names;
        this.types = types;
        this.rows = rows;

        // First occurrence wins on a duplicated column name, matching SqlDataReader.
        exactOrdinals = new Dictionary<string, int>(names.Length, StringComparer.Ordinal);
        looseOrdinals = new Dictionary<string, int>(names.Length, StringComparer.OrdinalIgnoreCase);
        for (var ordinal = 0; ordinal < names.Length; ordinal++)
        {
            exactOrdinals.TryAdd(names[ordinal], ordinal);
            looseOrdinals.TryAdd(names[ordinal], ordinal);
        }
    }

    /// <summary>Columns in the order the source returned them.</summary>
    public IReadOnlyList<string> ColumnNames => names;

    public int FieldCount => names.Length;

    public int RowCount => rows.Count;

    /// <summary>
    /// Reads <paramref name="reader"/> to completion into memory.
    ///
    /// <para><b>This is the first cancellation point inside an ingest anywhere in the engine.</b>
    /// The blocking drain checks the token once per row, so a lost write-gate lease stops a
    /// buffering source at the next row rather than at the command timeout. The remote round trip
    /// that produces row one is still uncancellable — that is the source's own API, not ours.</para>
    /// </summary>
    /// <param name="onRowsBuffered">
    /// Called with an INCREMENT (never a running total) roughly every
    /// <see cref="ProgressInterval"/> rows and once at the end, so a bounded admission window can
    /// watch the backlog grow while the fetch is still running.
    /// </param>
    public static BufferedRowSet Drain(
        DbDataReader reader,
        CancellationToken cancellationToken = default,
        Action<int>? onRowsBuffered = null)
    {
        var fieldCount = reader.FieldCount;
        var names = new string[fieldCount];
        var types = new Type[fieldCount];
        for (var ordinal = 0; ordinal < fieldCount; ordinal++)
        {
            names[ordinal] = reader.GetName(ordinal);
            types[ordinal] = reader.GetFieldType(ordinal) ?? typeof(object);
        }

        var rows = new List<object?[]>();
        var sinceReport = 0;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new object?[fieldCount];
            for (var ordinal = 0; ordinal < fieldCount; ordinal++)
                values[ordinal] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
            rows.Add(values);

            if (++sinceReport < ProgressInterval)
                continue;

            onRowsBuffered?.Invoke(sinceReport);
            sinceReport = 0;
        }

        if (sinceReport > 0)
            onRowsBuffered?.Invoke(sinceReport);

        return new BufferedRowSet(names, types, rows);
    }

    /// <summary>
    /// A forward-only reader over the buffered rows — the shape
    /// <see cref="SqlViewSnapshotIngestor.Ingest(SnapshotStore, DbDataReader, SqlViewSnapshotIngestorOptions)"/>
    /// consumes, so the serial half of a two-phase source runs the SAME code a one-phase source
    /// runs, with the same ordinal resolution and the same failure by name.
    /// </summary>
    public DbDataReader CreateReader() => new BufferedReader(this);

    private sealed class BufferedReader : DbDataReader
    {
        private readonly BufferedRowSet set;
        private int index = -1;
        private bool closed;

        public BufferedReader(BufferedRowSet set) => this.set = set;

        public override int Depth => 0;
        public override int FieldCount => set.names.Length;
        public override bool HasRows => set.rows.Count > 0;
        public override bool IsClosed => closed;
        public override int RecordsAffected => -1;

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read()
        {
            if (closed || index + 1 >= set.rows.Count)
            {
                index = set.rows.Count;
                return false;
            }

            index++;
            return true;
        }

        public override bool NextResult() => false;

        public override void Close() => closed = true;

        public override string GetName(int ordinal) => set.names[ordinal];

        public override Type GetFieldType(int ordinal) => set.types[ordinal];

        public override string GetDataTypeName(int ordinal) => set.types[ordinal].Name;

        /// <summary>
        /// Exact match first, then case-insensitive — the order <c>SqlDataReader</c> uses, so a
        /// view whose column casing differs from the table definition binds exactly as it does
        /// today. An unmatched name throws <see cref="IndexOutOfRangeException"/> carrying the
        /// name, which is the "read the view contract" tripwire for a renamed or dropped column.
        /// </summary>
        public override int GetOrdinal(string name)
        {
            if (set.exactOrdinals.TryGetValue(name, out var exact))
                return exact;
            if (set.looseOrdinals.TryGetValue(name, out var loose))
                return loose;
            throw new IndexOutOfRangeException(name);
        }

        public override bool IsDBNull(int ordinal) => Current[ordinal] is null;

        public override object GetValue(int ordinal) => Current[ordinal] ?? DBNull.Value;

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var ordinal = 0; ordinal < count; ordinal++)
                values[ordinal] = GetValue(ordinal);
            return count;
        }

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            var source = (byte[])GetValue(ordinal);
            if (buffer is null)
                return source.LongLength;

            var available = (int)Math.Max(0, Math.Min(length, source.LongLength - dataOffset));
            Array.Copy(source, dataOffset, buffer, bufferOffset, available);
            return available;
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            var source = GetValue(ordinal) switch
            {
                string text => text.ToCharArray(),
                char[] characters => characters,
                var other => throw new InvalidCastException(
                    $"Column '{GetName(ordinal)}' holds {other.GetType().Name}, which is not character data."),
            };

            if (buffer is null)
                return source.LongLength;

            var available = (int)Math.Max(0, Math.Min(length, source.LongLength - dataOffset));
            Array.Copy(source, dataOffset, buffer, bufferOffset, available);
            return available;
        }

        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

        private object?[] Current => index >= 0 && index < set.rows.Count
            ? set.rows[index]
            : throw new InvalidOperationException(
                "No row is current — call Read() first, and stop when it returns false.");
    }
}
