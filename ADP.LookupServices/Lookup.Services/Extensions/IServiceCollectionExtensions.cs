using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddLookupService<T>(this IServiceCollection services, LookupOptions options = null)
        where T : CosmosClient
    {
        if (options is not null)
            services.AddScoped(x => options);

        services.AddScoped<VehicleLookupService>();
        services.AddScoped<PartLookupService>();
        services.AddScoped<ServiceLookupService>();
        services.AddScoped<IIdentityCosmosService>(x => new IdentityCosmosService(x.GetRequiredService<T>()));
        services.AddScoped<IVehicleLoockupCosmosService>(x => new VehicleLoockupCosmosService(x.GetRequiredService<T>()));
        services.AddScoped<ILogCosmosService>(x => new LogCosmosService(x.GetRequiredService<T>()));
        services.AddScoped(x => new PartLookupCosmosService(x.GetRequiredService<T>()));
        services.AddScoped(x => new TBPCosmosService(x.GetRequiredService<T>()));
        services.AddScoped(x => new ServiceCosmosService(x.GetRequiredService<T>()));

        return services;
    }

    public static IServiceCollection AddLookupService<T>(this IServiceCollection services, Action<LookupOptions> optionsBuilder)
        where T : CosmosClient
    {
        var options = new LookupOptions();
        optionsBuilder.Invoke(options);

        services.AddLookupService<T>(options);

        return services;
    }
}
