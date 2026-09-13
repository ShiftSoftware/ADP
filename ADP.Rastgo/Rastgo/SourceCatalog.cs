using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Bounds metadata discovery. Catalog implementations must never read or return record values.</summary>
public sealed class SourceCatalogRequest
{
    public int MaxDatasets { get; init; } = 200;
    public int MaxFieldsPerDataset { get; init; } = 200;
    public int MaxFileDepth { get; init; } = 4;
    public int MaxFiles { get; init; } = 500;
    public IReadOnlyList<MeasureSpec> ReferencedMeasures { get; init; } = [];
    public IReadOnlyList<string> ExcludedTopLevelFolders { get; init; } = [];
}

/// <summary>Optional companion to <see cref="ICheckSource"/> for safe, metadata-only discovery.</summary>
public interface ICheckSourceCatalog
{
    Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct);
}

public sealed record CatalogField(string Name, string Type, bool? Nullable = null);

/// <summary>A table, view, container, or file. Detail must contain metadata only.</summary>
public sealed record CatalogDataset(
    string Name,
    string Kind,
    string? Namespace,
    IReadOnlyList<CatalogField> Fields,
    string? Detail = null)
{
    /// <summary>True when this exact dataset matched a path explicitly used by the loaded check pack.</summary>
    public bool Referenced { get; init; }

    /// <summary>File metadata used by compact file-share authoring output; null for non-file datasets.</summary>
    public long? SizeBytes { get; init; }
    public DateTimeOffset? ModifiedAtUtc { get; init; }
}

/// <summary>
/// One source's safe metadata snapshot. Connection details and record values do not belong here;
/// <paramref name="Error"/> and <paramref name="Note"/> must also be safe for display.
/// </summary>
public sealed record SourceCatalogSnapshot(
    string SourceName,
    string Provider,
    IReadOnlyList<CatalogDataset> Datasets,
    DateTimeOffset DiscoveredAtUtc,
    bool Truncated = false,
    string? Note = null,
    string? Error = null,
    bool FromCache = false)
{
    /// <summary>
    /// Top-level folders omitted from broad discovery. Explicit pack references still take priority
    /// and can be catalogued from an excluded folder.
    /// </summary>
    public IReadOnlyList<string> ExcludedTopLevelFolders { get; init; } = [];
}

public sealed record AuthoringCatalog(
    string PackName,
    DateTimeOffset GeneratedAtUtc,
    AuthoringCheckPack Pack,
    IReadOnlyList<SourceCatalogSnapshot> Sources,
    TimeSpan CacheDuration);

/// <summary>
/// Host-declared metadata for schemas that cannot be discovered safely, such as CSV headers or
/// logical document types inside a schema-less container. Hints must never contain record values.
/// </summary>
public sealed record SourceCatalogHint(string SourceName, CatalogDataset Dataset);

/// <summary>Discovery limits and in-memory cache behaviour for the live authoring page.</summary>
public sealed class AuthoringCatalogOptions
{
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ErrorCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxDatasetsPerSource { get; set; } = 200;
    public int MaxFieldsPerDataset { get; set; } = 200;
    public int MaxFileDepth { get; set; } = 4;
    public int MaxFiles { get; set; } = 500;
    public List<string> ExcludedFileShareTopLevelFolders { get; } = [];
    public List<SourceCatalogHint> CatalogHints { get; } = [];
}

/// <summary>
/// Builds the host-aware authoring model. Source failures are isolated and cached for a short period;
/// successful discovery uses the configured TTL. <paramref name="forceRefresh"/> bypasses both.
/// </summary>
public sealed class AuthoringCatalogService(SourceRegistry sources, RastgoOptions options)
{
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new(StringComparer.OrdinalIgnoreCase);

    public async Task<AuthoringCatalog> BuildAsync(
        string yamlText,
        string packName,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var pack = YamlCheckLoader.LoadForAuthoring(yamlText, sources.Names);
        var tasks = sources.Sources
            .OrderBy(source => source.Name, StringComparer.OrdinalIgnoreCase)
            .Select(source => GetSourceAsync(source, pack.Checks, forceRefresh, ct));
        var snapshots = await Task.WhenAll(tasks);
        var enriched = snapshots.Select(ApplyHints).ToList();
        return new(packName, DateTimeOffset.UtcNow, pack, enriched, options.Authoring.CacheDuration);
    }

    private SourceCatalogSnapshot ApplyHints(SourceCatalogSnapshot snapshot)
    {
        var hints = options.Authoring.CatalogHints
            .Where(hint => string.Equals(hint.SourceName, snapshot.SourceName, StringComparison.OrdinalIgnoreCase))
            .Select(hint => hint.Dataset)
            .ToList();
        if (hints.Count == 0) return snapshot;

        var datasets = snapshot.Datasets.ToList();
        foreach (var hint in hints)
        {
            var index = datasets.FindIndex(dataset =>
                string.Equals(dataset.Name, hint.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dataset.Kind, hint.Kind, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dataset.Namespace, hint.Namespace, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                datasets.Add(hint);
                continue;
            }

            var discovered = datasets[index];
            var fields = discovered.Fields.Concat(hint.Fields)
                .GroupBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            datasets[index] = discovered with
            {
                Fields = fields,
                Detail = hint.Detail ?? discovered.Detail,
                Referenced = discovered.Referenced || hint.Referenced,
            };
        }
        return snapshot with { Datasets = datasets };
    }

    private async Task<SourceCatalogSnapshot> GetSourceAsync(
        ICheckSource source,
        IReadOnlyList<CheckDefinition> checks,
        bool forceRefresh,
        CancellationToken ct)
    {
        var relevant = checks.SelectMany(c => c.Measures)
            .Where(m => string.Equals(m.Source, source.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var key = source.Name + ":" + Fingerprint(relevant);
        var now = DateTimeOffset.UtcNow;
        if (!forceRefresh && cache.TryGetValue(key, out var hit) && hit.ExpiresAtUtc > now)
            return hit.Snapshot with { FromCache = true };

        var gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (!forceRefresh && cache.TryGetValue(key, out hit) && hit.ExpiresAtUtc > now)
                return hit.Snapshot with { FromCache = true };

            SourceCatalogSnapshot snapshot;
            if (source is not ICheckSourceCatalog catalog)
            {
                snapshot = new(
                    source.Name,
                    source.GetType().Name,
                    [],
                    now,
                    Note: "This registered source does not provide catalog metadata.");
            }
            else
            {
                try
                {
                    snapshot = await catalog.DiscoverAsync(new SourceCatalogRequest
                    {
                        MaxDatasets = Math.Max(1, options.Authoring.MaxDatasetsPerSource),
                        MaxFieldsPerDataset = Math.Max(1, options.Authoring.MaxFieldsPerDataset),
                        MaxFileDepth = Math.Max(0, options.Authoring.MaxFileDepth),
                        MaxFiles = Math.Max(1, options.Authoring.MaxFiles),
                        ReferencedMeasures = relevant,
                        ExcludedTopLevelFolders = options.Authoring.ExcludedFileShareTopLevelFolders,
                    }, ct);
                    snapshot = snapshot with { SourceName = source.Name, FromCache = false };
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    snapshot = new(
                        source.Name,
                        source.GetType().Name,
                        [],
                        now,
                        Error: SafeError(ex));
                }
            }

            var ttl = snapshot.Error is null ? options.Authoring.CacheDuration : options.Authoring.ErrorCacheDuration;
            cache[key] = new(snapshot, now + (ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero));
            return snapshot;
        }
        finally
        {
            gate.Release();
        }
    }

    private static string Fingerprint(IEnumerable<MeasureSpec> measures)
    {
        // SQL is deliberately excluded: it can contain literals that should never be retained in cache keys.
        var metadataReferences = measures
            .Select(m => string.Join("|", m.Path ?? "", m.Database ?? "", m.Container ?? ""))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", metadataReferences)));
        return Convert.ToHexString(bytes.AsSpan(0, 8));
    }

    private static string SafeError(Exception ex)
    {
        // Connector exceptions can echo a connection string. Return only the type and a neutral message.
        return ex switch
        {
            UnauthorizedAccessException => "Metadata discovery was not authorized.",
            DirectoryNotFoundException => "The configured catalog root is unavailable.",
            FileNotFoundException => "The configured catalog resource is unavailable.",
            HttpRequestException when HasInner<System.Security.Authentication.AuthenticationException>(ex) =>
                "Metadata discovery could not establish a secure connection to this source.",
            HttpRequestException => "Metadata discovery could not reach this source over HTTP.",
            _ => $"Metadata discovery failed ({ex.GetType().Name}).",
        };
    }

    private static bool HasInner<T>(Exception exception) where T : Exception
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
            if (current is T) return true;
        return false;
    }

    private sealed record CacheEntry(SourceCatalogSnapshot Snapshot, DateTimeOffset ExpiresAtUtc);
}
