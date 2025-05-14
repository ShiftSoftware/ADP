using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.SyncAgent.Services;

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
}
