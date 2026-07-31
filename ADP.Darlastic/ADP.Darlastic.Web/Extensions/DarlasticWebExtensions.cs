using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ADP.Darlastic.Web.Extensions;

public static class DarlasticWebExtensions
{
    /// <summary>
    /// Registers Darlastic Blazor services into the consumer's DI container (the Menus/Surveys
    /// module pattern). The consumer is responsible for AddShiftBlazor, AddShiftIdentity,
    /// AddTypeAuth, HttpClient, etc. — this only wires Darlastic's options, action tree, and
    /// routing-discovery assembly.
    /// </summary>
    public static IServiceCollection AddDarlasticBlazorServices(
        this IServiceCollection services,
        Action<DarlasticWebOptions>? configure = null)
    {
        var options = new DarlasticWebOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        // Register DarlasticActionTree so the consumer doesn't have to — unless it gates entirely
        // on its own actions, in which case the module's tree is dead weight in the permissions UI.
        if (options.RegisterDarlasticActionTree)
            services.Configure<TypeAuthBlazorOptions>(o => o.AddActionTree<DarlasticActionTree>());

        // Register ADP.Darlastic.Web assembly for Blazor routing discovery
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(DarlasticWebExtensions).Assembly));

        return services;
    }
}
