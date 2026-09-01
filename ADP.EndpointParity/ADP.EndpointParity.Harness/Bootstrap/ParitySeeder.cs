using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Applies a group's adversarial seed with <b>explicit long primary keys</b>.
///
/// <para>
/// <b>Why explicit ids, and why this class exists at all.</b> Rule 1 refuses to normalize IDs away,
/// because IDs are the payload of trap 2 — a link row's PK leaking into a child's <c>ID</c> comes
/// back as a well-formed, plausible hash id, and only comparison against a known-good value
/// distinguishes it from correct. Rule 1 buys that by making ids DETERMINISTIC instead: same pinned
/// salt + same seeded long ⇒ same hash id on every run, so a seeded id compares literally.
/// That whole chain collapses if the database picks the ids.
/// </para>
///
/// <para>
/// <b>Mechanism: <c>SET IDENTITY_INSERT</c>, deliberately, over <c>ValueGeneratedNever()</c>.</b>
/// The alternative would mean editing a module's own <c>IModelBuildingContributor</c> — production
/// source, changed to suit a harness that Step 08 deletes. This needs no source change at all.
/// Verified working against a real ShiftEntity table (SPIKE-3): explicit <c>ID = 5000001</c> came
/// back through the live HTTP pipeline as its pinned-salt hash.
/// </para>
///
/// <para>
/// <b>Note on schemas:</b> ShiftEntity modules place their tables under their OWN SQL schema, not
/// <c>dbo</c> — <c>[Surveys].[Survey]</c>, singular. Each seed file states the schema explicitly.
/// </para>
/// </summary>
public sealed class ParitySeeder
{
    private readonly string connectionString;

    public ParitySeeder(string connectionString) => this.connectionString = connectionString;

    /// <summary>
    /// Markers of hostile rows that were seeded, in file order. The summary gate checks these
    /// appear LITERALLY in list bodies — which is the gate that replaces "&gt; 0 rows", because
    /// a row-count gate is satisfied by a sample host's own demo seed and therefore cannot
    /// distinguish "the adversarial seed was applied" from "only demo data is present".
    /// </summary>
    public List<string> HostileMarkers { get; } = new();

    /// <summary>Every seeded row's id, keyed by "schema.table", for building DETAIL cases.</summary>
    public Dictionary<string, List<long>> SeededIds { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Applies <c>Seed/&lt;group&gt;.seed.json</c>. Shape:
    /// <code>
    /// {
    ///   "group": "Surveys",
    ///   "tables": [
    ///     { "schema": "Surveys", "table": "Survey", "rows": [
    ///        { "ID": 5000001, "Name": "…", "_hostile": ["trap1"], "_marker": "PARITY-H1" }
    ///     ]}
    ///   ]
    /// }
    /// </code>
    /// Keys beginning with <c>_</c> are harness metadata and are never written to SQL. Tables are
    /// applied in file order, so a seed lists parents before the children that reference them.
    /// </summary>
    public async Task ApplyAsync(string seedFilePath, CancellationToken ct = default)
    {
        var text = await File.ReadAllTextAsync(seedFilePath, ct);
        var root = JsonNode.Parse(text)!.AsObject();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var tableNode in root["tables"]!.AsArray())
        {
            var table = tableNode!.AsObject();
            var schema = table["schema"]!.GetValue<string>();
            var name = table["table"]!.GetValue<string>();
            var qualified = "[" + schema + "].[" + name + "]";

            var rows = table["rows"]!.AsArray();
            if (rows.Count == 0) continue;

            foreach (var rowNode in rows)
            {
                var row = rowNode!.AsObject();

                if (row.TryGetPropertyValue("_marker", out var marker) && marker is not null)
                    HostileMarkers.Add(marker.GetValue<string>());

                if (row.TryGetPropertyValue("ID", out var idNode) && idNode is not null)
                {
                    var key = schema + "." + name;
                    if (!SeededIds.TryGetValue(key, out var ids))
                        SeededIds[key] = ids = new List<long>();
                    ids.Add(idNode.GetValue<long>());
                }

                var columns = row.Where(p => !p.Key.StartsWith('_')).ToList();

                var columnList = string.Join(", ", columns.Select(c => "[" + c.Key + "]"));
                var paramList = string.Join(", ", columns.Select((_, i) => "@p" + i));

                // IDENTITY_INSERT is scoped to the session and to ONE table at a time, so it is
                // toggled per statement rather than held open across the whole seed.
                await using var cmd = new SqlCommand(
                    "SET IDENTITY_INSERT " + qualified + " ON; " +
                    "INSERT INTO " + qualified + " (" + columnList + ") VALUES (" + paramList + "); " +
                    "SET IDENTITY_INSERT " + qualified + " OFF;", conn);

                for (var i = 0; i < columns.Count; i++)
                    cmd.Parameters.AddWithValue("@p" + i, ToSqlValue(columns[i].Value));

                try
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                catch (SqlException ex)
                {
                    // Loud, with the row echoed. A silently-skipped seed row is a trap that never
                    // fires and a harness that reports green over no coverage at all.
                    throw new InvalidOperationException(
                        "Parity seed failed on " + qualified + " row " + row.ToJsonString() +
                        " -> " + ex.Message, ex);
                }
            }
        }
    }

    private static object ToSqlValue(JsonNode? node)
    {
        if (node is null) return DBNull.Value;

        if (node is JsonValue value)
        {
            if (value.TryGetValue<bool>(out var b)) return b;
            if (value.TryGetValue<long>(out var l)) return l;
            if (value.TryGetValue<decimal>(out var d)) return d;
            if (value.TryGetValue<string>(out var s)) return s;
        }

        // Objects and arrays are JSON columns (several of these entities store polymorphic
        // schemas as a JSON blob), so they go in as their serialized text.
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Drops and recreates the run's database so every capture starts from an identical state.
    /// Rule 1's "same longs both runs" is only true if nothing survives between runs.
    /// </summary>
    public static async Task ResetDatabaseAsync(string masterConnectionString, string database, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(masterConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "IF DB_ID(@db) IS NOT NULL BEGIN " +
            "  ALTER DATABASE [" + database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            "  DROP DATABASE [" + database + "]; END", conn);
        cmd.Parameters.AddWithValue("@db", database);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
