using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>A source column on a snapshot table (bookkeeping columns are implicit).</summary>
/// <param name="Name">Column name. Must be a plain identifier; may not start with <c>_</c> (reserved for bookkeeping).</param>
/// <param name="DuckDbType">DuckDB column type, e.g. <c>VARCHAR</c>, <c>BIGINT</c>, <c>DECIMAL(18,4)</c>, <c>TIMESTAMP</c>.</param>
public sealed record SnapshotColumn(string Name, string DuckDbType);

/// <summary>
/// Defines one consolidated snapshot table under the <c>data</c> schema: the source columns a
/// family's ingestor stages, to which Hawta appends the <see cref="BookkeepingColumns"/>.
/// </summary>
public class SnapshotTableDefinition
{
    private static readonly Regex IdentifierPattern = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex TypePattern = new(@"^[A-Za-z][A-Za-z0-9_ ]*(\(\s*\d+\s*(,\s*\d+\s*)?\))?$", RegexOptions.Compiled);

    internal static bool IsValidIdentifier(string name) => IdentifierPattern.IsMatch(name);

    public string Name { get; }
    public IReadOnlyList<SnapshotColumn> Columns { get; }
    /// <summary>
    /// The persisted columns that can affect a destination document. Ingestors hash these
    /// separately from <see cref="Columns"/>, allowing source-only changes to be retained
    /// without re-enqueueing Cosmos replication.
    /// </summary>
    public IReadOnlyList<SnapshotColumn> ReplicationColumns { get; }

    /// <summary>The optional audit-only column populated only when raw-source capture is explicitly enabled.</summary>
    public string? RawSourceColumn { get; }

    public SnapshotTableDefinition(string name, IReadOnlyList<SnapshotColumn> columns)
        : this(name, columns, columns, rawSourceColumn: null)
    {
    }

    protected SnapshotTableDefinition(
        string name,
        IReadOnlyList<SnapshotColumn> columns,
        IReadOnlyList<SnapshotColumn> replicationColumns,
        string? rawSourceColumn)
    {
        if (!IdentifierPattern.IsMatch(name))
            throw new ArgumentException($"'{name}' is not a valid snapshot table name.", nameof(name));
        if (columns.Count == 0)
            throw new ArgumentException("A snapshot table needs at least one source column.", nameof(columns));

        foreach (var column in columns)
        {
            if (!IdentifierPattern.IsMatch(column.Name))
                throw new ArgumentException($"'{column.Name}' is not a valid column name (identifiers only; leading '_' is reserved for bookkeeping columns).");
            if (!TypePattern.IsMatch(column.DuckDbType))
                throw new ArgumentException($"'{column.DuckDbType}' is not a valid DuckDB column type for '{column.Name}'.");
        }

        var duplicates = columns.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new ArgumentException($"Duplicate column name(s): {string.Join(", ", duplicates)}.");

        if (replicationColumns.Count == 0)
            throw new ArgumentException("A snapshot table needs at least one replication-tracked column.", nameof(replicationColumns));

        var columnNames = columns.Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownReplicationColumns = replicationColumns
            .Where(column => !columnNames.Contains(column.Name))
            .Select(column => column.Name)
            .ToList();
        if (unknownReplicationColumns.Count > 0)
            throw new ArgumentException(
                $"Replication column(s) are not stored by the table: {string.Join(", ", unknownReplicationColumns)}.",
                nameof(replicationColumns));
        if (rawSourceColumn is not null && !columnNames.Contains(rawSourceColumn))
            throw new ArgumentException($"Raw source column '{rawSourceColumn}' is not stored by the table.", nameof(rawSourceColumn));
        if (rawSourceColumn is not null
            && replicationColumns.Any(column => column.Name.Equals(rawSourceColumn, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("The raw source column cannot participate in replication change tracking.", nameof(rawSourceColumn));

        Name = name;
        Columns = columns;
        ReplicationColumns = replicationColumns;
        RawSourceColumn = rawSourceColumn;
    }

    /// <summary>Fully-qualified, quoted table name under the <c>data</c> schema.</summary>
    public string QualifiedName => $"data.\"{Name}\"";

    internal string QuotedColumnList => string.Join(", ", Columns.Select(c => $"\"{c.Name}\""));
}
