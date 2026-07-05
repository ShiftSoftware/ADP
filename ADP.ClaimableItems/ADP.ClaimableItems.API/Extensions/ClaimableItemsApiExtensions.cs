using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

using DataMarker = ShiftSoftware.ADP.ClaimableItems.Data.Marker;
using SharedMarker = ShiftSoftware.ADP.ClaimableItems.Shared.Marker;

namespace ShiftSoftware.ADP.ClaimableItems.API.Extensions;

public static class ClaimableItemsApiExtensions
{
    /// <summary>
    /// Registers the Claimable Items catalog API services into the consumer's DI container.
    /// The consumer is responsible for AddControllers, AddShiftEntityWeb, AddShiftIdentity,
    /// AddTypeAuth, etc. This method only wires up the module's own repositories, options,
    /// route convention, entity model contributor, and application part.
    ///
    /// Follows the ShiftDbContext registration variant (not a module-specific DbContext interface):
    /// module services resolve the abstract <see cref="ShiftDbContext"/> aliased to the consumer's
    /// own context, so a consumer whose context does not implement any module interface still works.
    /// </summary>
    public static IServiceCollection AddClaimableItemsApiServices<TDbContext>(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder,
        Action<ClaimableItemsApiOptions> configure)
        where TDbContext : ShiftDbContext
    {
        var options = new ClaimableItemsApiOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        services.AddScoped<ShiftDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.RegisterShiftRepositories(typeof(DataMarker).Assembly);

        // Register the module's assemblies so the consumer doesn't have to. Both Data AND Shared
        // are added as data assemblies because the FluentValidation validators live colocated with
        // the DTOs in the Shared project (dual-assembly scan).
        services.Configure<ShiftEntityOptions>(o =>
        {
            o.AddDataAssembly(typeof(DataMarker).Assembly);
            o.AddDataAssembly(typeof(SharedMarker).Assembly);
            o.AddAutoMapper(typeof(DataMarker).Assembly);
        });

        if (options.RegisterModuleActionTree)
            services.Configure<TypeAuthAspNetCoreOptions>(o => o.AddActionTree<ClaimableItemsActionTree>());

        // Contribute the module's entity configuration to the consumer's DbContext at model-build time.
        // The schema is captured here (null for TCA → keep dbo; "ClaimableItems" for the sample).
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(options.Schema));

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new ClaimableItemsRoutePrefixConvention(options.RoutePrefix));
        });

        mvcBuilder.AddApplicationPart(typeof(ClaimableItemsApiExtensions).Assembly);

        // Slice 3 registers the catalog repositories (ClaimableItem / Campaign / CampaignVinEntry) here.

        return services;
    }

    public static IServiceCollection AddClaimableItemsApiServices<TDbContext>(
        this IServiceCollection services,
        ClaimableItemsApiOptions options,
        IMvcBuilder mvcBuilder)
        where TDbContext : ShiftDbContext
        => services.AddClaimableItemsApiServices<TDbContext>(mvcBuilder, o => CopyOptions(options, o));

    private static void CopyOptions(ClaimableItemsApiOptions source, ClaimableItemsApiOptions target)
    {
        if (source is null) return;
        target.RoutePrefix = source.RoutePrefix;
        target.EnableClaimableItemsActionTreeAuthorization = source.EnableClaimableItemsActionTreeAuthorization;
        target.RegisterModuleActionTree = source.RegisterModuleActionTree;
        target.Schema = source.Schema;
        target.ClaimableItemSetupAction = source.ClaimableItemSetupAction;
        target.CampaignVinEntriesAction = source.CampaignVinEntriesAction;
    }
}
