using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace ShiftSoftware.ADP.Rastgo.Extensions;

/// <summary>
/// Composition root for Rastgo. A host calls <see cref="AddRastgoCore(IServiceCollection, Action{RastgoOptions})"/>
/// and then opts into the sources its check packs use (<c>AddRastgoDuckDb</c>, <c>AddRastgoCosmos</c>). Each source
/// registers an <see cref="ICheckSource"/>; <see cref="SourceRegistry"/> collects them and resolves by
/// <see cref="ICheckSource.Name"/> at run time. This only packages the wiring <c>HealthRunner/Program.cs</c> does by
/// hand — the contracts are unchanged.
/// <para>
/// The framework currently ships as a single package, but the source registrations stay connector-aligned so the
/// DuckDB / Cosmos sources can be peeled into their own packages later (for dependency isolation) without changing
/// this DI surface.
/// </para>
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddRastgoCore(this IServiceCollection services, Action<RastgoOptions> configure)
    {
        var options = new RastgoOptions();
        configure(options);
        return services.AddRastgoCore(options);
    }

    public static IServiceCollection AddRastgoCore(this IServiceCollection services, RastgoOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<ICheckSource>(_ => new FileShareCheckSource(options.FileShareBase, options.ConflictCopyMarker));
        services.AddSingleton(sp => new SourceRegistry(sp.GetServices<ICheckSource>()));
        services.AddSingleton<CheckRunner>();
        services.AddSingleton(_ => new JsonlResultSink(options.ResultsRoot));
        return services;
    }

    /// <summary>
    /// Registers the read-only DuckDB check source (source name <c>duckdb</c>) — typically pointed at the published
    /// read snapshot. Chain after <c>AddRastgoCore</c>.
    /// </summary>
    public static IServiceCollection AddRastgoDuckDb(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<ICheckSource>(_ => new DuckDbCheckSource(connectionString));
        return services;
    }

    /// <summary>
    /// Registers the read-only Cosmos check source (source name <c>cosmos</c>) from a connection string. A
    /// null/blank string registers a no-client source: cosmos checks report a source error but the run still
    /// completes (mirrors the incubated HealthRunner behaviour). Chain after <c>AddRastgoCore</c>.
    /// </summary>
    public static IServiceCollection AddRastgoCosmos(this IServiceCollection services, string? connectionString)
    {
        var client = string.IsNullOrWhiteSpace(connectionString) ? null : new CosmosClient(connectionString);
        return services.AddRastgoCosmos(client);
    }

    /// <summary>Registers the read-only Cosmos check source from an already-configured client (or null).</summary>
    public static IServiceCollection AddRastgoCosmos(this IServiceCollection services, CosmosClient? client)
    {
        services.AddSingleton<ICheckSource>(_ => new CosmosCheckSource(client));
        return services;
    }
}
