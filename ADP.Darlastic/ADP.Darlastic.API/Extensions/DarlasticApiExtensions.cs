using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Darlastic.Data.Extensions;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

public static class DarlasticApiExtensions
{
    /// <summary>
    /// Registers Darlastic API services into the consumer's DI container (the Menus/Surveys
    /// module pattern). The consumer is responsible for AddControllers, AddShiftEntityWeb,
    /// AddShiftIdentity, AddTypeAuth, etc. — this only wires Darlastic's own model contributor
    /// (so the host DbContext picks up the registry entities + golden view without registering
    /// it manually), route convention, action tree, and application part.
    ///
    /// The resolve ENGINE stays out of process (a host's sync agent / the dev spike) — these
    /// endpoints only read what it writes, which is why there are no repositories here.
    /// </summary>
    public static IServiceCollection AddDarlasticApiServices<TDbContext>(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder,
        Action<DarlasticApiOptions>? configure = null)
        where TDbContext : ShiftDbContext
    {
        var options = new DarlasticApiOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        services.AddScoped<ShiftDbContext>(sp => sp.GetRequiredService<TDbContext>());

        // Registry model (tables + the GoldenCustomer view) lands in the host's DbContext model;
        // the host's own migrations create and version everything under options.Schema.
        services.AddSingleton<IModelBuildingContributor>(new DarlasticModelBuildingContributor(options.Schema));

        services.Configure<TypeAuthAspNetCoreOptions>(o => o.AddActionTree<DarlasticActionTree>());

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new DarlasticRoutePrefixConvention(options.RoutePrefix));
        });

        mvcBuilder.AddApplicationPart(typeof(DarlasticApiExtensions).Assembly);

        return services;
    }
}
