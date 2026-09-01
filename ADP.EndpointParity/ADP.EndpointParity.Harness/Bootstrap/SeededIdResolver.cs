using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Resolves the HASH ids of seeded rows by asking the application for them.
///
/// <para>
/// The seed writes explicit LONG primary keys (Rule 1), but every URL the harness issues needs the
/// hash id the application would render. Rather than reimplement the hash — which would mean
/// pulling ShiftEntity's hashing into the harness and pinning the harness to one version of it —
/// this reads the ids straight off a list response. The application is the authority on how its
/// own ids are encoded.
/// </para>
///
/// <para>
/// Safe because of Rule 1's own guarantees: the list carries an explicit <c>$orderby=ID</c>, the
/// seed's longs are fixed, and the database is fresh, so the returned order is the seeded order and
/// the mapping long-&gt;hash is stable across runs. If it ever were not, the stability gate (two
/// captures diffing to empty) is what would catch it.
/// </para>
/// </summary>
public static class SeededIdResolver
{
    /// <summary>
    /// Fetches every id the entity's list endpoint returns, in <c>$orderby=ID</c> order.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        HttpClient client, string routePrefix, string entity, CancellationToken ct = default)
    {
        var url = "/" + routePrefix.Trim('/') + "/" + entity + "?$orderby=ID&$top=5&$count=true";
        var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return Array.Empty<string>();

        var text = await response.Content.ReadAsStringAsync(ct);
        var node = Canonical.TryParse(text);
        if (node is not JsonObject obj) return Array.Empty<string>();

        // The framework's OData envelope is { "Count": n, "Value": [ ... ] }.
        if (!obj.TryGetPropertyValue("Value", out var value) || value is not JsonArray rows)
            return Array.Empty<string>();

        var ids = new List<string>();
        foreach (var row in rows)
        {
            if (row is JsonObject r &&
                r.TryGetPropertyValue("ID", out var id) &&
                id is JsonValue v && v.TryGetValue<string>(out var s) && s is not null)
                ids.Add(s);
        }

        return ids;
    }
}
