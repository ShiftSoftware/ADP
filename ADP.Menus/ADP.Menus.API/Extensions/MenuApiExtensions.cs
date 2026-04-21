using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ADP.Menus.Data.Extensions;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

namespace ShiftSoftware.ADP.Menus.API.Extensions;

public static class MenuApiExtensions
{
    /// <summary>
    /// Registers Menu API services into the consumer's DI container.
    /// The consumer is responsible for calling AddControllers, AddShiftEntityWeb,
    /// AddShiftIdentity, AddTypeAuth, etc. This method only wires up Menu's
    /// own repositories, options, route convention, and application part.
    /// </summary>
    public static IServiceCollection AddMenuApiServices<TDbContext>(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder,
        Action<MenuApiOptions> configure)
        where TDbContext : ShiftDbContext
    {
        var options = new MenuApiOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        services.AddScoped<ShiftDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.RegisterShiftRepositories(typeof(Data.Marker).Assembly);

        // Register Menu's assemblies so the consumer doesn't have to
        services.Configure<ShiftEntityOptions>(o =>
        {
            o.AddDataAssembly(typeof(Data.Marker).Assembly);
            o.AddAutoMapper(typeof(Data.Marker).Assembly);
        });

        services.Configure<TypeAuthAspNetCoreOptions>(o => o.AddActionTree<MenuActionTree>());

        // Register Menu entity configuration so the consumer's DbContext doesn't need to call ConfigureMenuEntities()
        services.AddSingleton<IModelBuildingContributor, MenuModelBuildingContributor>();

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new MenuRoutePrefixConvention(options.RoutePrefix));
        });

        mvcBuilder.AddApplicationPart(typeof(MenuApiExtensions).Assembly);

        services.AddScoped<ReplacementItemRepository>();
        services.AddScoped<VehicleModelRepository>();
        services.AddScoped<MenuRepository>();
        services.AddScoped<MenuVariantRepository>();
        services.AddScoped<ServiceIntervalRepository>();
        services.AddScoped<ServiceIntervalGroupRepository>();
        services.AddScoped<LabourRateMappingRepository>();
        services.AddScoped<BrandMappingRepository>();
        services.AddScoped<StandaloneReplacementItemGroupRepository>();

        return services;
    }

    public static IServiceCollection AddMenuApiServices<TDbContext>(
        this IServiceCollection services,
        MenuApiOptions options,
        IMvcBuilder mvcBuilder)
        where TDbContext : ShiftDbContext
        => services.AddMenuApiServices<TDbContext>(mvcBuilder, o => CopyMenuApiOptions(options, o));

    private static void CopyMenuApiOptions(MenuApiOptions source, MenuApiOptions target)
    {
        if (source is null) return;
        target.RoutePrefix = source.RoutePrefix;
        target.EnableMenuActionTreeAuthorization = source.EnableMenuActionTreeAuthorization;
        target.Languages = source.Languages;
    }
}
