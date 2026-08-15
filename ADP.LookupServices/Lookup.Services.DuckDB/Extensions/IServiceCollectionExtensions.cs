using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDuckDBLookupServices(this IServiceCollection services)
    {
        services.AddScoped<IVehicleLookupStorageService>(x => new DuckDBVehicleLookupStorageService(
            x.GetRequiredService<global::DuckDB.NET.Data.DuckDBConnection>(),
            x.GetRequiredService<IHashIdService>()));
        services.AddScoped<IVehicleReportService, DuckDBVehicleReportService>();

        return services;
    }

    /// <summary>
    /// Makes DuckDB the menu lookup's storage, over the host-registered <c>DuckDBConnection</c> — the
    /// same connection the vehicle DuckDB services use. This is the ONLY call a DuckDB host adds on
    /// top of its usual menu registration: it REPLACES the default Cosmos storage
    /// (<c>AddServiceMenuLookup</c> registers Cosmos only while no storage has been chosen), so after
    /// registration the container holds exactly one menu storage, in any call order.
    /// </summary>
    public static IServiceCollection AddDuckDBServiceMenuLookup(this IServiceCollection services)
        => services.AddDuckDBServiceMenuLookup<global::DuckDB.NET.Data.DuckDBConnection>();

    /// <summary>
    /// Same, against a specific <c>DuckDBConnection</c> registration — for hosts that keep more than
    /// one (say, separate lookup databases) and register each under its own derived type, exactly as
    /// <c>AddServiceMenuLookup&lt;TCosmosClient&gt;</c> selects among multiple Cosmos clients.
    /// </summary>
    public static IServiceCollection AddDuckDBServiceMenuLookup<TConnection>(this IServiceCollection services)
        where TConnection : global::DuckDB.NET.Data.DuckDBConnection
    {
        RemoveExistingMenuStorage(services);

        services.AddScoped<IServiceMenuLookupStorageService>(x => new DuckDBServiceMenuLookupStorageService(
            x.GetRequiredService<TConnection>()));

        return services;
    }

    /// <summary>
    /// Same, from a connection string — for hosts with no <c>DuckDBConnection</c> registration at all.
    /// Each scope's reader opens (and disposes) its own connection to that database, so point the
    /// string at a published read snapshot and prefer <c>access_mode=read_only</c>.
    /// </summary>
    public static IServiceCollection AddDuckDBServiceMenuLookup(this IServiceCollection services, string connectionString)
    {
        RemoveExistingMenuStorage(services);

        services.AddScoped<IServiceMenuLookupStorageService>(_ => new DuckDBServiceMenuLookupStorageService(connectionString));

        return services;
    }

    /// <summary>
    /// Choosing DuckDB UNREGISTERS the menu storage chosen so far (the Cosmos default and its concrete
    /// reader) rather than merely shadowing it — after this call the container holds exactly one menu
    /// storage. In the DuckDB-first order there is nothing to remove, and <c>AddServiceMenuLookup</c>
    /// then sees a storage exists and never adds its Cosmos default.
    /// </summary>
    private static void RemoveExistingMenuStorage(IServiceCollection services)
    {
        services.RemoveAll<IServiceMenuLookupStorageService>();
        services.RemoveAll<ServiceMenuCosmosService>();
    }

    // No registration for DuckDBServiceMenuSyncService yet, on purpose: the sync is a placeholder
    // (NotImplementedException), and a resolvable registration would make a host believe the feature
    // exists. The real sync brings its own registration when it lands.
}
