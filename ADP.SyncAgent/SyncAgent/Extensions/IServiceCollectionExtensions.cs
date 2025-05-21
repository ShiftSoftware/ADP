using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSyncAgent(this IServiceCollection services, SyncAgentOptions options)
    {
        services.AddScoped(x => options);
        services.AddScoped<CSVSyncServiceFactory>();
        return services;
    }

    public static IServiceCollection AddSyncAgent(this IServiceCollection services, Action<SyncAgentOptions> optionsProvider)
    {
        var options = new SyncAgentOptions();
        optionsProvider(options);
        services.AddSyncAgent(options);
        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource(this IServiceCollection services, CSVSyncDataSourceOptions options)
    {
        services.AddScoped(x => options);
        services.AddTransient(typeof(CSVSyncDataSource<,>));

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource(this IServiceCollection services, Action<CSVSyncDataSourceOptions> optionsProvider)
    {
        var options = new CSVSyncDataSourceOptions();
        optionsProvider(options);
        services.AddCSVSyncDataSource(options);

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource<TStorageService>(this IServiceCollection services, CSVSyncDataSourceOptions options)
        where TStorageService : class, IStorageService
    {
        services.AddCSVSyncDataSource(options);
        services.AddScoped<IStorageService, TStorageService>();

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource<TStorageService>(this IServiceCollection services, Action<CSVSyncDataSourceOptions> optionsProvider)
        where TStorageService : class, IStorageService
    {
        var options = new CSVSyncDataSourceOptions();
        optionsProvider(options);
        services.AddCSVSyncDataSource<TStorageService>(options);

        return services;
    }

    public static IServiceCollection AddCosmosSyncDataDestination<TCosmosClient>(this IServiceCollection services)
        where TCosmosClient : CosmosClient
    {
        services.AddSingleton<SyncCosmosClient>(x => new(x.GetRequiredService<TCosmosClient>()));
        services.AddTransient(typeof(CosmosSyncDataDestination<,>));

        return services;
    }
}
