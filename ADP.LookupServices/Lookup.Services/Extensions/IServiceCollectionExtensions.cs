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
        services.AddSingleton<LookUpCosmosClient>(x => new LookUpCosmosClient(x.GetRequiredService<TCosmosClient>(), options?.CosmosDatabaseNameSuffix));

        if (options is not null)
            services.AddScoped(x => options);

        services.AddScoped<VehicleLookupService>();
        services.AddScoped<PartLookupService>();
        services.AddScoped<ServiceLookupService>();
        services.AddScoped<GoldenCustomerLookupService>();
        services.AddScoped<IIdentityCosmosService>(x => new IdentityCosmosService(x.GetRequiredService<LookUpCosmosClient>()));

        if (options.VehicleLookupStorageSource == Enums.StorageSources.CosmosDB)
        {
            services.AddScoped<IVehicleLookupStorageService>(x => new CosmosVehicleLookupStorageService(x.GetRequiredService<LookUpCosmosClient>()));
        }

        services.AddScoped<ILogCosmosService>(x => new LogCosmosService(x.GetRequiredService<LookUpCosmosClient>()));
        services.AddScoped(x => new PartLookupCosmosService(x.GetRequiredService<LookUpCosmosClient>(), options));
        services.AddScoped(x => new ServiceCosmosService(x.GetRequiredService<LookUpCosmosClient>()));

        // Service menus ARE registered here now: they are part of the vehicle lookup result
        // (VehicleLookupDTO.ServiceMenu), so the vehicle lookup has to be able to resolve them. This is safe
        // for a deployment that never provisioned the menu containers — registration touches no Cosmos, and
        // the section is only read when a request opts in with ServiceMenuOptions.Include, which reports the
        // fault rather than raising it. Everything inside is TryAdd, so a host that also calls
        // AddServiceMenuLookup itself still gets exactly one registration.
        //
        // ConfigureServiceMenu is forwarded so the menu settings are reachable from the ONE call every host
        // already makes. Registering menus without a way to configure them here would leave the dangerous
        // configuration as the default: country 0 and no CountrySettingsResolver, which charges a
        // single-country deployment the wrong labour rate (open item O6) unless the host happens to know a
        // second registration call exists.
        services.AddServiceMenuLookup<TCosmosClient>(options?.ConfigureServiceMenu);

        // One suffix, two option classes. A dev pointing the whole lookup at "-alt" databases means the menu
        // containers too; the menu options simply do not know about LookupOptions. Coalescing rather than
        // assigning means an explicit suffix — set in ConfigureServiceMenu above, or in an
        // AddServiceMenuLookup(o => …) call in either order — still wins.
        services.AddOptions<ServiceMenuLookupOptions>()
                .Configure(menuOptions => menuOptions.CosmosDatabaseNameSuffix ??= options?.CosmosDatabaseNameSuffix);

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
