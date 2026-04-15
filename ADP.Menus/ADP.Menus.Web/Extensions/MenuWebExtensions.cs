using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Web.Services;
using ShiftSoftware.ADP.Menus.Web.WebServices;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ADP.Menus.Web.Extensions;

public static class MenuWebExtensions
{
    /// <summary>
    /// Registers Menu Blazor services into the consumer's DI container.
    /// The consumer is responsible for calling AddShiftBlazor, AddShiftIdentity,
    /// AddTypeAuth, registering HttpClient, and any other infrastructure.
    /// This method only wires up Menu's own web services, options, and auth helper.
    /// </summary>
    public static IServiceCollection AddMenuBlazorServices(
        this IServiceCollection services,
        Action<MenuWebOptions>? configure = null)
    {
        var options = new MenuWebOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register MenuActionTree so the consumer doesn't have to
        services.Configure<TypeAuthBlazorOptions>(o => o.AddActionTree<MenuActionTree>());

        // Register ADP.Menus.Web assembly for Blazor routing discovery
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(MenuWebExtensions).Assembly));

        services.AddScoped<ReplacementItemService>();
        services.AddScoped<ServiceIntervalService>();
        services.AddScoped<ServiceIntervalGroupService>();
        services.AddScoped<MenuService>();
        services.AddScoped<MenuAuthService>();

        return services;
    }
}
