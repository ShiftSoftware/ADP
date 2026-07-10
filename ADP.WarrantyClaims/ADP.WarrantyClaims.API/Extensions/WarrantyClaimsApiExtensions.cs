using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

using DataMarker = ShiftSoftware.ADP.WarrantyClaims.Data.Marker;
using SharedMarker = ShiftSoftware.ADP.WarrantyClaims.Shared.Marker;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Extensions;

public static class WarrantyClaimsApiExtensions
{
    /// <summary>
    /// Registers the warranty claims API services into the consumer's DI container. The consumer is
    /// responsible for AddControllers, AddShiftEntityWeb, AddShiftIdentity, AddTypeAuth, etc. This method
    /// only wires up the module's own repositories, options, route convention, entity model contributor,
    /// and application part.
    ///
    /// Follows the ShiftDbContext registration variant: module services resolve the abstract
    /// <see cref="ShiftDbContext"/> aliased to the consumer's own context.
    /// </summary>
    public static IServiceCollection AddWarrantyClaimsApiServices<TDbContext>(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder,
        Action<WarrantyClaimsApiOptions> configure)
        where TDbContext : ShiftDbContext
    {
        var options = new WarrantyClaimsApiOptions();
        configure?.Invoke(options);

        if (configure is not null)
            services.Configure(configure);

        services.AddScoped<ShiftDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.RegisterShiftRepositories(typeof(DataMarker).Assembly);

        // Register the module's assemblies so the consumer doesn't have to. Both Data AND Shared are
        // added as data assemblies because the FluentValidation validators live colocated with the DTOs
        // in the Shared project (dual-assembly scan).
        services.Configure<ShiftEntityOptions>(o =>
        {
            o.AddDataAssembly(typeof(DataMarker).Assembly);
            o.AddDataAssembly(typeof(SharedMarker).Assembly);
            o.AddAutoMapper(typeof(DataMarker).Assembly);
        });

        // Contribute the module's entity configuration to the consumer's DbContext at model-build time.
        // The schema is captured here (null for the original host application → keep dbo).
        services.AddSingleton<ShiftSoftware.ShiftEntity.EFCore.IModelBuildingContributor>(new WarrantyClaimsModelBuildingContributor(options.Schema));

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new WarrantyClaimsRoutePrefixConvention(options.RoutePrefix));
        });

        // Print seams (Phase 3 Slice 3.2). The date formatter ships a module default; TryAdd makes a
        // consumer registration win deterministically — an earlier consumer AddScoped turns this into
        // a no-op, and a later one wins at resolution time (MS DI resolves the last registration).
        // ICompanyInfoProvider deliberately has NO default: print methods throw with registration
        // guidance when it is missing.
        services.TryAddScoped<IPrintoutDateFormatter>(_ => new DefaultPrintoutDateFormatter());

        // Module defaults for the Phase-3.3 consumer seams (Phase 3 Slice 3.6, D24). TryAdd, same
        // deterministic swap contract as the date formatter above: a consumer registration made
        // BEFORE this call wins (e.g. rates kept in an org-private store, exports on blob storage);
        // one made after wins at resolution time. The rates default persists into the module-owned
        // WarrantyRates entity; the storage default writes under the content root's
        // warranty-csv-exports folder.
        services.TryAddScoped<ShiftSoftware.ADP.WarrantyClaims.Shared.IWarrantyRatesStore, Data.Services.DefaultWarrantyRatesStore>();
        services.TryAddScoped<ShiftSoftware.ADP.WarrantyClaims.Shared.IWarrantyCsvExportStorage, Services.DefaultWarrantyCsvExportStorage>();

        // Per-report frx override paths, captured as a singleton value object so the Data-layer print
        // methods can read it without referencing this options type (same capture-at-registration
        // pattern as the model-building contributor above). All-null = embedded defaults.
        services.AddSingleton(options.ReportOverrides);

        mvcBuilder.AddApplicationPart(typeof(WarrantyClaimsApiExtensions).Assembly);

        return services;
    }
}
