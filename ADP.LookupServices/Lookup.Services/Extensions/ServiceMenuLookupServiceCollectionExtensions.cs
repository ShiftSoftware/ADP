using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Extensions;

/// <summary>
/// Registers the service-menu lookup.
///
/// <para><b>Separate from <c>AddLookupService</c> on purpose.</b> Service menus are a self-contained
/// feature over their own Cosmos containers — a host can want menus without the vehicle lookup, or the
/// vehicle lookup without menus — so turning them on is its own call with its own
/// <see cref="ServiceMenuLookupOptions"/>. The general lookup registration will call this itself in a
/// later phase, once menus are part of the vehicle lookup result; until then a host opts in here.</para>
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

        // Its own LookUpCosmosClient rather than the container-registered one: the two registrations are
        // independent and each carries its own database-name suffix, so sharing a single instance would
        // make the resolved suffix depend on which registration ran first. The wrapper is trivial and
        // both point at the same CosmosClient.
        services.TryAddScoped(x => new ServiceMenuCosmosService(
            new LookUpCosmosClient(
                x.GetRequiredService<TCosmosClient>(),
                x.GetRequiredService<IOptions<ServiceMenuLookupOptions>>().Value?.CosmosDatabaseNameSuffix)));

        services.TryAddScoped<ServiceMenuGenerationEvaluator>();
        services.TryAddScoped<ServiceMenuLookupService>();

        return services;
    }
}
