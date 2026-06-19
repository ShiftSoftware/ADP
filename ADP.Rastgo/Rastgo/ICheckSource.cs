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

/// <summary>Resolves a source by its <see cref="ICheckSource.Name"/>.</summary>
public sealed class SourceRegistry
{
    private readonly Dictionary<string, ICheckSource> _sources;

    public SourceRegistry(IEnumerable<ICheckSource> sources)
        => _sources = sources.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    public ICheckSource? Resolve(string name) => _sources.GetValueOrDefault(name);
}
