namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The uniform row-hash recipe: md5 over the canonicalized text of the listed columns, in the
/// listed order. Every ingestor computes <c>_RowHash</c> with this expression so hashes stay
/// comparable across source formats (CSV vs parquet vs SQL) for the same family.
/// </summary>
public static class RowHash
{
    /// <summary>
    /// Builds the DuckDB SQL expression producing the row hash. Each column is cast to VARCHAR
    /// and length-prefixed (<c>v{length}:{value}</c>) so field boundaries are unambiguous even
    /// when a value contains the join separator; NULL canonicalizes to a bare sentinel that no
    /// prefixed value can collide with.
    /// </summary>
    public static string Expression(IEnumerable<string> columnNames)
    {
        var parts = columnNames
            .Select(name =>
            {
                if (!SnapshotTableDefinition.IsValidIdentifier(name))
                    throw new ArgumentException($"'{name}' is not a valid column name for the row hash.", nameof(columnNames));
                return $"coalesce('v' || length(CAST(\"{name}\" AS VARCHAR)) || ':' || CAST(\"{name}\" AS VARCHAR), chr(0))";
            })
            .ToList();

        if (parts.Count == 0)
            throw new ArgumentException("At least one column is required.", nameof(columnNames));

        return $"md5(concat_ws(chr(31), {string.Join(", ", parts)}))";
    }
}
