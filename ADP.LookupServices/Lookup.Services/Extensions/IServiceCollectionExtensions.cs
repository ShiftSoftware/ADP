using DuckDB.NET.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddLookupService(this IServiceCollection services, LookupOptions options = null)
    {
        if (options is not null)
            services.AddScoped(x => options);

        services.AddScoped<VehicleLookupService>();
        services.AddScoped<PartLookupService>();
        services.AddScoped<ServiceLookupService>();
        services.AddScoped<IIdentityCosmosService>(x => new IdentityCosmosService(x.GetRequiredService<CosmosClient>()));

        if (options?.VehicleLookupStorageSource == Enums.StorageSources.CosmosDB)
        {
            services.AddScoped<IVehicleLoockupStorageService>(x => new CosmosVehicleLoockupStorageService(x.GetRequiredService<CosmosClient>()));
        }
        else if (options?.VehicleLookupStorageSource == Enums.StorageSources.DuckDB)
        {
            services.AddScoped<IVehicleLoockupStorageService>(x => new DuckDBVehicleLoockupStorageService(x.GetRequiredService<DuckDBConnection>()));
        }

        services.AddScoped<ILogCosmosService>(x => new LogCosmosService(x.GetRequiredService<CosmosClient>()));
        services.AddScoped(x => new PartLookupCosmosService(x.GetRequiredService<CosmosClient>(), options));
        //services.AddScoped(x => new TBPCosmosService(x.GetRequiredService<T>()));
        services.AddScoped(x => new ServiceCosmosService(x.GetRequiredService<CosmosClient>()));

        return services;
    }

    public static IServiceCollection AddLookupService(this IServiceCollection services, Action<LookupOptions> optionsBuilder)
    {
        var options = new LookupOptions();

        optionsBuilder.Invoke(options);

        services.AddLookupService(options);

        return services;
    }
}