using ADP.SyncAgent.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ADP.SyncAgent.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSyncAgent(this IServiceCollection services, SyncAgentOptions options)
    {
        services.AddScoped(x=> options);
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
}
