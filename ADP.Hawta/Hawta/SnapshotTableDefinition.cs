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

    public SnapshotTableDefinition(string name, IReadOnlyList<SnapshotColumn> columns)
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

        Name = name;
        Columns = columns;
    }

    /// <summary>Fully-qualified, quoted table name under the <c>data</c> schema.</summary>
    public string QualifiedName => $"data.\"{Name}\"";

    internal string QuotedColumnList => string.Join(", ", Columns.Select(c => $"\"{c.Name}\""));
}
