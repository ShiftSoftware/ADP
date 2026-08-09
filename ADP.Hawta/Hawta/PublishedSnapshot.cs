using System.Text.RegularExpressions;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A published read snapshot, opened read-only through its views-shim. This is the consumer
/// side of the publish contract: resolve the newest <c>{SnapshotName}-{ts}.duckdb</c> shim,
/// open it, query the same <c>data.*</c> table names the write DB has. The shim's views hold
/// bare relative parquet filenames (the set is relocatable across local dir / SMB / abfss),
/// so <see cref="Open"/> sets DuckDB's <c>file_search_path</c> to the shim's directory;
/// plain <c>duckdb.exe</c> users get the same effect by opening the shim from inside the
/// publish directory.
/// </summary>
public sealed class PublishedSnapshot : IDisposable
{
    public string ShimPath { get; }
    public DuckDBConnection Connection { get; }

    private PublishedSnapshot(string shimPath, DuckDBConnection connection)
    {
        ShimPath = shimPath;
        Connection = connection;
    }

    /// <summary>Full path of the newest shim in the directory, or null when none is published yet.</summary>
    public static string? ResolveNewest(string publishDirectory, string snapshotName) =>
        ListShims(publishDirectory, snapshotName).FirstOrDefault();

    /// <summary>All shim paths in the directory, newest first (ordinal name order — the stamp format makes that chronological).</summary>
    internal static IReadOnlyList<string> ListShims(string publishDirectory, string snapshotName)
    {
        if (!Directory.Exists(publishDirectory))
            return [];

        var pattern = ShimPattern(snapshotName);
        // Directory enumeration patterns are case-sensitive on Linux. Keep the narrow shim
        // search while requesting case-insensitive matching so configured-name casing drift has
        // the same behavior on local disks, SMB, and CI.
        return Directory.EnumerateFiles(publishDirectory, $"{snapshotName}-*.duckdb", new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
        })
            .Where(path => pattern.IsMatch(Path.GetFileName(path)))
            .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // IgnoreCase mirrors the filesystem's semantics (Windows local disk and the SMB share are
    // both case-insensitive): a shim whose on-disk casing differs from the configured name
    // must never become invisible to baseline/retention while its parquet stays sweepable.
    internal static Regex ShimPattern(string snapshotName) =>
        new($"^{Regex.Escape(snapshotName)}-([0-9]{{17}})\\.duckdb$", RegexOptions.IgnoreCase);

    public static PublishedSnapshot? OpenNewest(string publishDirectory, string snapshotName)
    {
        var newest = ResolveNewest(publishDirectory, snapshotName);
        return newest is null ? null : Open(newest);
    }

    public static PublishedSnapshot Open(string shimPath)
    {
        var connection = new DuckDBConnection($"Data Source={shimPath};ACCESS_MODE=READ_ONLY");
        try
        {
            connection.Open();

            var directory = Path.GetDirectoryName(Path.GetFullPath(shimPath))!;
            using var command = connection.CreateCommand();
            command.CommandText = $"SET file_search_path = '{directory.Replace("'", "''")}'";
            command.ExecuteNonQuery();

            return new PublishedSnapshot(shimPath, connection);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public PublishInfo ReadInfo()
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "PublishId", "SnapshotName", "PublishedAt", "SchemaVersion", "PackageVersion"
            FROM meta.publish_info
            """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            throw new InvalidDataException($"'{ShimPath}' has no meta.publish_info row — not a Hawta views-shim.");

        return new PublishInfo(
            reader.GetString(0), reader.GetString(1),
            DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
            reader.GetInt32(3), reader.GetString(4));
    }

    public IReadOnlyList<PublishedTableManifest> ReadManifest()
    {
        var entries = new List<PublishedTableManifest>();

        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Table", "ParquetFile", "RowCount", "StateHash", "MaxLastModified", "ExportedAt"
            FROM meta.publish_manifest ORDER BY "Table"
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new PublishedTableManifest(
                Table: reader.GetString(0),
                ParquetFile: reader.GetString(1),
                RowCount: reader.GetInt64(2),
                StateHash: reader.GetString(3),
                MaxLastModified: reader.IsDBNull(4) ? null : DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                ExportedAt: DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)));
        }

        return entries;
    }

    public void Dispose() => Connection.Dispose();
}

public sealed record PublishInfo(
    string PublishId, string SnapshotName, DateTime PublishedAt, int SchemaVersion, string PackageVersion);

/// <summary>
/// One table's entry in a shim's <c>meta.publish_manifest</c>: which parquet file carries it,
/// plus the change signature (<paramref name="RowCount"/> + <paramref name="StateHash"/>, the
/// per-row-state XOR aggregate) it was exported at. <paramref name="MaxLastModified"/> is
/// observability — "data as of".
/// </summary>
public sealed record PublishedTableManifest(
    string Table,
    string ParquetFile,
    long RowCount,
    string StateHash,
    DateTime? MaxLastModified,
    DateTime ExportedAt);
