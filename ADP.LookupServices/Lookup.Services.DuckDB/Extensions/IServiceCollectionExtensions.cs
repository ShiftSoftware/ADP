using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDuckDBLookupServices(this IServiceCollection services)
    {
        services.AddScoped<IVehicleLookupStorageService>(x => new DuckDBVehicleLookupStorageService(x.GetRequiredService<global::DuckDB.NET.Data.DuckDBConnection>()));
        services.AddScoped<IVehicleReportService, DuckDBVehicleReportService>();

        return services;
    }
}
