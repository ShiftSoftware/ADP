using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Extensions;

/// <summary>
/// Registers the service-menu lookup.
///
/// <para><b>Its own call, and still worth having.</b> <c>AddLookupService</c> now calls this itself — service
/// menus are part of the vehicle lookup result — and forwards
/// <see cref="LookupOptions.ConfigureServiceMenu"/>, so a host with both features configures everything in
/// one place. This call remains the entry point for menus <i>without</i> the vehicle lookup, and composes
/// with the general registration in either order: both are <c>Configure</c> actions on the same options
/// builder, applied in registration order.</para>
///
/// <para><b>Storage: one registration, ever.</b> This call registers the storage-agnostic core (the
/// lookup service, the evaluator, the options) and, only when no
/// <see cref="IServiceMenuLookupStorageService"/> has been chosen yet, the Cosmos reader as the DEFAULT
/// storage. Choosing another backend is a registration, not an option: the DuckDB package's
/// <c>AddDuckDBServiceMenuLookup(…)</c> removes the Cosmos default and puts its own storage in its
/// place, so whichever backend the host ends up with is the ONLY storage in the container — in any
/// call order, with nothing dead left behind.</para>
///
/// <para>Everything registers with <c>TryAdd</c>, so calling this alongside (or twice with) another
/// registration is safe and the first one wins.</para>
/// </summary>
public static class ServiceMenuLookupServiceCollectionExtensions
{
    /// <summary>Registers the service-menu lookup against the default <see cref="CosmosClient"/>.</summary>
    public static IServiceCollection AddServiceMenuLookup(this IServiceCollection services, Action<ServiceMenuLookupOptions> configure = null)
        => services.AddServiceMenuLookup<CosmosClient>(configure);

    /// <summary>
    /// Registers the service-menu lookup against a specific <see cref="CosmosClient"/> registration, for
    /// hosts that keep more than one.
    /// </summary>
    /// <param name="configure">
    /// Optional. For settings that need the host's own services, use
    /// <c>services.AddOptions&lt;ServiceMenuLookupOptions&gt;().Configure&lt;TDependency&gt;(…)</c>
    /// instead — nothing here takes an <see cref="IServiceProvider"/>.
    /// </param>
    public static IServiceCollection AddServiceMenuLookup<TCosmosClient>(this IServiceCollection services, Action<ServiceMenuLookupOptions> configure = null)
        where TCosmosClient : CosmosClient
    {
        var builder = services.AddOptions<ServiceMenuLookupOptions>();

        if (configure is not null)
            builder.Configure(configure);

        // The DEFAULT storage: Cosmos, registered only while no storage has been chosen. An already
        // present storage — the DuckDB package's, or a host's own — is an explicit choice and is never
        // overridden, and the Cosmos reader is not even registered beside it. Every call order lands on
        // exactly one storage:
        //   • menus only                        → Cosmos, the default a host gets for doing nothing special
        //   • menus first, then AddDuckDB…      → the DuckDB call removes this default and adds its own
        //   • AddDuckDB… first, then menus      → a storage already exists, so the default is skipped
        //   • this method called again later    → same — the surviving storage stays whatever was chosen
        if (!services.Any(x => x.ServiceType == typeof(IServiceMenuLookupStorageService)))
        {
            // Its own LookUpCosmosClient rather than the container-registered one: the two registrations are
            // independent and each carries its own database-name suffix, so sharing a single instance would
            // make the resolved suffix depend on which registration ran first. The wrapper is trivial and
            // both point at the same CosmosClient.
            services.TryAddScoped(x => new ServiceMenuCosmosService(
                new LookUpCosmosClient(
                    x.GetRequiredService<TCosmosClient>(),
                    x.GetRequiredService<IOptions<ServiceMenuLookupOptions>>().Value?.CosmosDatabaseNameSuffix)));

            services.TryAddScoped<IServiceMenuLookupStorageService>(x => x.GetRequiredService<ServiceMenuCosmosService>());
        }

        services.TryAddScoped<ServiceMenuGenerationEvaluator>();
        services.TryAddScoped<ServiceMenuLookupService>();

        return services;
    }
}
