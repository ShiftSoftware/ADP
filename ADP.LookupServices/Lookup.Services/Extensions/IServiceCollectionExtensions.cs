using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddLookupService(this IServiceCollection services, LookupOptions options = null)
        => services.AddLookupService<CosmosClient>(options);

    public static IServiceCollection AddLookupService(this IServiceCollection services, Action<LookupOptions> optionsBuilder)
        => services.AddLookupService<CosmosClient>(optionsBuilder);

    public static IServiceCollection AddLookupService<TCosmosClient>(this IServiceCollection services, LookupOptions options = null)
        where TCosmosClient : CosmosClient
    {
        services.AddSingleton<LookUpCosmosClient>(x => new LookUpCosmosClient(x.GetRequiredService<TCosmosClient>()));

        if (options is not null)
            services.AddScoped(x => options);

        services.AddScoped<VehicleLookupService>();
        services.AddScoped<PartLookupService>();
        services.AddScoped<ServiceLookupService>();
        services.AddScoped<IIdentityCosmosService>(x => new IdentityCosmosService(x.GetRequiredService<LookUpCosmosClient>()));

        if (options.VehicleLookupStorageSource == Enums.StorageSources.CosmosDB)
        {
            services.AddScoped<IVehicleLookupStorageService>(x => new CosmosVehicleLookupStorageService(x.GetRequiredService<LookUpCosmosClient>()));
        }

        services.AddScoped<ILogCosmosService>(x => new LogCosmosService(x.GetRequiredService<LookUpCosmosClient>()));
        services.AddScoped(x => new PartLookupCosmosService(x.GetRequiredService<LookUpCosmosClient>(), options));
        services.AddScoped(x => new ServiceCosmosService(x.GetRequiredService<LookUpCosmosClient>()));

        return services;
    }

    public static IServiceCollection AddLookupService<TCosmosClient>(this IServiceCollection services, Action<LookupOptions> optionsBuilder)
        where TCosmosClient : CosmosClient
    {
        var options = new LookupOptions();

        optionsBuilder.Invoke(options);

        return services.AddLookupService<TCosmosClient>(options);
    }
}
