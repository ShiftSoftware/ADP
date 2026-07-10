using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Printing;
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
        // The schema is captured here (null for the original host application → keep dbo; "ClaimableItems" for the sample).
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(options.Schema));

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new ClaimableItemsRoutePrefixConvention(options.RoutePrefix));
        });

        // Print seams (Phase 3 Slice 3.2). The date formatter ships a module default; TryAdd makes a
        // consumer registration win deterministically — an earlier consumer AddScoped turns this into
        // a no-op, and a later one wins at resolution time (MS DI resolves the last registration).
        // ICompanyInfoProvider deliberately has NO default: print methods throw with registration
        // guidance when it is missing.
        services.TryAddScoped<IPrintoutDateFormatter>(_ => new DefaultPrintoutDateFormatter());

        // Per-report frx override paths, captured as a singleton value object so the Data-layer print
        // methods can read it without referencing this options type (same capture-at-registration
        // pattern as the model-building contributor above). All-null = embedded defaults.
        services.AddSingleton(options.ReportOverrides);

        // Data-layer consumer options (Phase 3 Slice 3.6), captured the same way: the ItemClaim
        // repository's CanModifyPostClaim default reads the consumer's TypeAuth node from this object.
        services.AddSingleton(new Data.ClaimableItemsDataOptions
        {
            PostClaimModificationAction = options.PostClaimModificationAction,
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
        target.ReportOverrides = source.ReportOverrides;
        target.RoutePrefix = source.RoutePrefix;
        target.EnableClaimableItemsActionTreeAuthorization = source.EnableClaimableItemsActionTreeAuthorization;
        target.RegisterModuleActionTree = source.RegisterModuleActionTree;
        target.Schema = source.Schema;
        target.ClaimableItemSetupAction = source.ClaimableItemSetupAction;
        target.CampaignVinEntriesAction = source.CampaignVinEntriesAction;
        target.ClaimingAction = source.ClaimingAction;
        target.CertifyingAction = source.CertifyingAction;
        target.InvoicingAction = source.InvoicingAction;
        target.PostClaimModificationAction = source.PostClaimModificationAction;
    }
}
