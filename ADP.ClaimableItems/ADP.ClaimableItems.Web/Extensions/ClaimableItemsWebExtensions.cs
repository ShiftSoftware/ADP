using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ADP.ClaimableItems.Web.Extensions;

public static class ClaimableItemsWebExtensions
{
    /// <summary>
    /// Registers the Claimable Items Blazor services into the consumer's DI container.
    /// The consumer is responsible for AddShiftBlazor, AddShiftIdentity, AddTypeAuth, HttpClient, etc.
    /// This method registers the module's action tree, exposes its assembly to the Blazor router,
    /// and (from Slice 5) its web services.
    /// </summary>
    public static IServiceCollection AddClaimableItemsBlazorServices(
        this IServiceCollection services,
        Action<ClaimableItemsWebOptions>? configure = null)
    {
        var options = new ClaimableItemsWebOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        // Register the module's default action tree only when the consumer opts in (fresh consumers /
        // sample). The original host application supplies its own action-tree nodes via the options, so it leaves this false (D9).
        if (options.RegisterModuleActionTree)
            services.Configure<TypeAuthBlazorOptions>(o => o.AddActionTree<ClaimableItemsActionTree>());

        // Expose the module assembly for Blazor routing discovery (AdditionalAssemblies) — this is what
        // makes the catalog pages (which keep their original routes) resolve from the consumer's router.
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(ClaimableItemsWebExtensions).Assembly));

        // Capability seam for the claim pages (Slice 7): consumers register their own adapter BEFORE
        // or AFTER this call (TryAdd keeps a consumer registration when it came first; a later
        // consumer AddScoped also wins at resolution). Default = dealer-safe (no distributor actions).
        services.TryAddScoped<IClaimableItemsCapabilityProvider, DefaultClaimableItemsCapabilityProvider>();

        return services;
    }
}
