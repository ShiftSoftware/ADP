using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ShiftBlazor.Extensions;

namespace ShiftSoftware.ADP.WarrantyClaims.Web.Extensions;

public static class WarrantyClaimsWebExtensions
{
    /// <summary>
    /// Registers the Warranty Claims Blazor services into the consumer's DI container.
    /// The consumer is responsible for AddShiftBlazor, AddShiftIdentity, AddTypeAuth, HttpClient, etc.
    /// This method exposes the module's assembly to the Blazor router and registers the web-side
    /// capability seam default. There is no module warranty action tree — the consumer supplies its own
    /// TypeAuth nodes through <see cref="WarrantyClaimsWebOptions"/> (D9).
    /// </summary>
    public static IServiceCollection AddWarrantyClaimsBlazorServices(
        this IServiceCollection services,
        Action<WarrantyClaimsWebOptions>? configure = null)
    {
        var options = new WarrantyClaimsWebOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        // Expose the module assembly for Blazor routing discovery (AdditionalAssemblies) — this is what
        // makes the warranty pages (which keep their original routes) resolve from the consumer's router.
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(WarrantyClaimsWebExtensions).Assembly));

        // Capability seam for the warranty pages: consumers register their own adapter BEFORE or AFTER
        // this call (TryAdd keeps a consumer registration when it came first; a later consumer AddScoped
        // also wins at resolution). Default = dealer-safe (no distributor actions).
        services.TryAddScoped<IWarrantyClaimsCapabilityProvider, DefaultWarrantyClaimsCapabilityProvider>();

        return services;
    }
}
