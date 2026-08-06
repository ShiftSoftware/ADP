namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// A queryable source of metrics (DuckDB, Cosmos, file share, …). Runs entirely read-only —
/// it queries the footprints the monitored pipelines already leave, never mutating them.
/// </summary>
public interface ICheckSource
{
    /// <summary>Name referenced by <see cref="MeasureSpec.Source"/> (e.g. "duckdb").</summary>
    string Name { get; }

    Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct);
}

/// <summary>
/// The convention for addressing several sources of the same kind. A host that has exactly one
/// SQL Server registers it bare — <c>sql</c> — and YAML says <c>source: sql</c>. A host that has
/// one connection string per tenant registers each under a qualified name — <c>sql:AAD</c>,
/// <c>sql:BYY</c> — and YAML names the one it means.
/// <para>
/// The qualifier lives in <see cref="ICheckSource.Name"/> rather than in <see cref="SourceRegistry"/>,
/// which is why the registry needs no special case: eight qualified sources are simply eight distinct
/// names. <see cref="SourceRegistry"/>'s duplicate-name guard therefore stays fully in force — it
/// still catches the genuine mistake of registering the same address twice.
/// </para>
/// <para>
/// There is deliberately NO fallback from <c>sql:AAD</c> to bare <c>sql</c>. A mistyped qualifier
/// must surface as "Unknown source", not silently bind to a different tenant's database and report
/// a confident, wrong Pass.
/// </para>
/// </summary>
public static class SourceName
{
    /// <summary>Separates the source kind from its qualifier.</summary>
    public const char Separator = ':';

    /// <summary>
    /// Composes <paramref name="kind"/> and <paramref name="qualifier"/> into a registry name.
    /// A null/blank qualifier yields the bare kind, so one code path serves both shapes.
    /// </summary>
    public static string Compose(string kind, string? qualifier)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Source kind is required.", nameof(kind));

        if (string.IsNullOrWhiteSpace(qualifier))
            return kind;

        var trimmed = qualifier.Trim();
        if (trimmed.Contains(Separator))
            throw new ArgumentException(
                $"Source qualifier '{trimmed}' must not contain '{Separator}'.", nameof(qualifier));

        return $"{kind}{Separator}{trimmed}";
    }
}

/// <summary>Resolves a source by its <see cref="ICheckSource.Name"/>.</summary>
public sealed class SourceRegistry
{
    private readonly Dictionary<string, ICheckSource> _sources;

    public SourceRegistry(IEnumerable<ICheckSource> sources)
    {
        _sources = new Dictionary<string, ICheckSource>(StringComparer.OrdinalIgnoreCase);

        // Not ToDictionary: its duplicate-key ArgumentException names the key but not what collided,
        // and the failure it reports — two sources registered under one name — is a wiring bug whose
        // whole diagnosis is "which two". Qualify one of them (see SourceName) if they are meant to
        // coexist. Throwing remains correct: last-one-wins would turn a loud startup failure into
        // permanently wrong check results.
        foreach (var source in sources)
        {
            if (!_sources.TryAdd(source.Name, source))
                throw new ArgumentException(
                    $"Two check sources are registered as '{source.Name}' " +
                    $"({_sources[source.Name].GetType().Name} and {source.GetType().Name}). " +
                    $"Give one a qualifier — e.g. '{source.Name}{SourceName.Separator}<name>' — so both can be addressed.",
                    nameof(sources));
        }
    }

    public ICheckSource? Resolve(string name) => _sources.GetValueOrDefault(name);

    /// <summary>Every registered name, for startup logging and diagnostics.</summary>
    public IReadOnlyCollection<string> Names => _sources.Keys;
}
