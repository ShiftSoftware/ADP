using Microsoft.Extensions.DependencyInjection;

namespace ShiftSoftware.ADP.Menus.Sync.Extensions;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ServiceMenuDuckDBSyncService"/>, the prebuilt SyncEngine flow that syncs
    /// the menu catalog from its SQL source of truth into the DuckDB menu tables the
    /// <c>ShiftSoftware.ADP.Lookup.Services.DuckDB</c> menu lookup reads. The host passes its own
    /// menus DbContext and the DuckDB WRITE connection per call — the service itself holds no state.
    /// Also registers the generated ShiftMapper mapper of <c>ADP.Menus.Data</c>, which holds the entity → row
    /// maps (<c>MenuDuckDBMapper</c>), so the host writes no mapping line of its own.
    /// </summary>
    public static IServiceCollection AddServiceMenuDuckDBSync(this IServiceCollection services)
    {
        services.AddShiftMapper(typeof(Data.Marker).Assembly);
        services.AddScoped<ServiceMenuDuckDBSyncService>();

        return services;
    }
}
