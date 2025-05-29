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
        services.AddTransient(typeof(FileHelperCsvSyncDataSource<,>));

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

    public static IServiceCollection AddEFCoreSyncDataSource(this IServiceCollection services)
    {
        services.AddTransient(typeof(EFCoreSyncDataSource< , >));
        services.AddTransient(typeof(EFCoreSyncDataSource<, , >));
        services.AddTransient(typeof(EFCoreSyncDataSource<, , , >));

        return services;
    }

    public static IServiceCollection AddCosmosSyncDataDestination(this IServiceCollection services)
    {
        services.AddTransient(typeof(CosmosSyncDataDestination<,>));
        services.AddTransient(typeof(CosmosSyncDataDestination<,,>));
        services.AddTransient(typeof(CosmosSyncDataDestination<,,,>));

        return services;
    }

    public static IServiceCollection AddSyncService(this IServiceCollection services)
    {
        services.AddTransient(typeof(SyncService<>));
        services.AddTransient(typeof(SyncService<,>));

        return services;
    }
}
