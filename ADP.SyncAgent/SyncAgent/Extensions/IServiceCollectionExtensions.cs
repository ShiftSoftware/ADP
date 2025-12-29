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

    public static IServiceCollection AddCSVSyncDataSource(this IServiceCollection services, FileSystemStorageOptions options)
    {
        services.AddScoped(x => options);
        services.AddTransient(typeof(FileHelperCsvSyncDataSource<,>));
        services.AddTransient(typeof(CsvHelperCsvSyncDataSource<,>));

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource(this IServiceCollection services, Action<FileSystemStorageOptions> optionsProvider)
    {
        var options = new FileSystemStorageOptions();
        optionsProvider(options);
        services.AddCSVSyncDataSource(options);

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource<TStorageService>(this IServiceCollection services, FileSystemStorageOptions options)
        where TStorageService : class, IStorageService
    {
        services.AddCSVSyncDataSource(options);
        services.AddScoped<IStorageService, TStorageService>();

        return services;
    }

    public static IServiceCollection AddCSVSyncDataSource<TStorageService>(this IServiceCollection services, Action<FileSystemStorageOptions> optionsProvider)
        where TStorageService : class, IStorageService
    {
        var options = new FileSystemStorageOptions();
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
        services.AddTransient(typeof(SyncEngine<>));
        services.AddTransient(typeof(SyncEngine<,>));

        return services;
    }

    public static IServiceCollection AddEFCoreSyncDataDestination(this IServiceCollection services)
    {
        services.AddTransient(typeof(EFCoreSyncDataDestination<,>));
        services.AddTransient(typeof(EFCoreSyncDataDestination<,,>));
        services.AddTransient(typeof(EFCoreSyncDataDestination<,,,>));

        return services;
    }
}
