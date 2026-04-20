using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.Data.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Repositories;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

namespace ShiftSoftware.ADP.Surveys.API.Extensions;

public static class SurveyApiExtensions
{
    /// <summary>
    /// Registers the Surveys API surface into the consumer's DI container. The consumer
    /// is still responsible for calling <c>AddControllers</c>, <c>AddShiftEntityWeb</c>,
    /// <c>AddShiftIdentity</c>, <c>AddTypeAuth</c>, etc. — this method only wires up
    /// what Surveys owns: its repositories, options, route convention, action tree,
    /// model-building contributor, and application part.
    ///
    /// Mirrors <c>AddMenuApiServices</c> 1:1 so operators who know Menus know Surveys.
    /// </summary>
    public static IServiceCollection AddSurveysApiServices<TDbContext>(
        this IServiceCollection services,
        SurveyApiOptions options,
        IMvcBuilder mvcBuilder)
        where TDbContext : ShiftDbContext
    {
        services.AddSingleton(options);
        services.AddScoped<ShiftDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.RegisterShiftRepositories(typeof(Data.Marker).Assembly);

        services.Configure<ShiftEntityOptions>(o =>
        {
            // Register both assemblies so FluentValidation auto-discovery finds validators
            // living alongside schema DTOs in Shared AND alongside admin DTOs wherever
            // they live (Shared today). AutoMapper profiles live in Data.
            o.AddDataAssembly(typeof(Data.Marker).Assembly);
            o.AddDataAssembly(typeof(Shared.Marker).Assembly);
            o.AddAutoMapper(typeof(Data.Marker).Assembly);
        });

        services.Configure<TypeAuthAspNetCoreOptions>(o => o.AddActionTree<SurveysActionTree>());

        // Lets the consumer's DbContext pick up Surveys' entity configuration without
        // having to call ConfigureSurveyEntities() themselves.
        services.AddSingleton<IModelBuildingContributor, SurveyModelBuildingContributor>();

        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.Conventions.Add(new SurveyRoutePrefixConvention(options.RoutePrefix));
        });

        mvcBuilder.AddApplicationPart(typeof(SurveyApiExtensions).Assembly);

        services.AddScoped<SurveyRepository>();
        services.AddScoped<BankQuestionRepository>();
        services.AddScoped<ScreenTemplateRepository>();

        // EfBankSource is scoped per request — each resolve pass gets a fresh cache.
        services.AddScoped<Data.Bank.EfBankSource>();

        // Explicitly register FluentValidation validators from Shared + Data. ShiftEntity's
        // model-binding layer uses its own `AddDataAssembly` path for per-DTO validation,
        // but PublishController asks for `IValidator<SurveyDto>` by DI — that resolution
        // path needs the validators in the regular IServiceCollection, which only happens
        // via AddValidatorsFromAssembly.
        services.AddValidatorsFromAssembly(typeof(Shared.Marker).Assembly, includeInternalTypes: true);
        services.AddValidatorsFromAssembly(typeof(Data.Marker).Assembly, includeInternalTypes: true);

        return services;
    }

    public static IServiceCollection AddSurveysApiServices<TDbContext>(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder,
        Action<SurveyApiOptions> configure)
        where TDbContext : ShiftDbContext
    {
        var options = new SurveyApiOptions();
        configure(options);
        return services.AddSurveysApiServices<TDbContext>(options, mvcBuilder);
    }
}
