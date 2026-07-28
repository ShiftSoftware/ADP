using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

        // Skippable so a host gating entirely on its own actions (see SurveyActionOverrides)
        // doesn't surface a second, unused tree in its permissions UI.
        if (options.RegisterSurveysActionTree)
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
        services.AddScoped<SurveyInstanceRepository>();
        services.AddScoped<Services.TriggerIngestService>();
        services.AddScoped<Services.TriggerSchedulerService>();
        services.AddScoped<Services.OutboxDispatchService>();
        services.AddScoped<Services.SurveyResponseExporter>();

        // Surfaces deployment settings whose defaults are only wrong in production —
        // the link template above all. Logs at startup; never throws.
        services.AddHostedService<Services.SurveyOptionsStartupCheck>();

        // Rate-limit policy for the anonymous endpoints. Registered unconditionally so the
        // policy name on PublicSurveyController always resolves; it is a no-op partition
        // unless the deployment sets a limit. Requires the host's app.UseRateLimiter().
        services.AddRateLimiter(rl =>
        {
            rl.AddPolicy(SurveysRateLimitPolicy.Name, httpContext =>
            {
                var limit = options.PublicEndpointRateLimit;
                if (limit is not > 0)
                    return RateLimitPartition.GetNoLimiter("adp-surveys-disabled");

                // Partition by caller IP. The instance id is the capability here, so there
                // is no user to key on — IP is the only signal available.
                var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit.Value,
                    Window = options.PublicEndpointRateLimitWindow,
                    QueueLimit = 0,
                });
            });
        });

        // Trigger channels — register the in-memory mock by default so dev / e2e harnesses
        // have a working channel out of the box. Real deployments add their WhatsApp / SMS
        // / email implementations alongside; SurveyChannelRegistry resolves by Key.
        services.AddSingleton<ShiftSoftware.ADP.Surveys.Shared.Triggers.ISurveyChannel, Channels.InMemorySurveyChannel>();
        services.AddSingleton<Channels.SurveyChannelRegistry>();

        // Outbox subscribers — same pattern. Real deployments register their Service Bus /
        // webhook / in-process subscribers; the mock keeps the dispatch path runnable in
        // dev / e2e. Slice 7.
        services.AddSingleton<ShiftSoftware.ADP.Surveys.Shared.Triggers.ISurveyResponseSubscriber, Subscribers.InMemoryOutboxSubscriber>();
        services.AddSingleton<Subscribers.OutboxSubscriberRegistry>();

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

    /// <summary>
    /// Registers pull-based trigger ingestion: the given <see cref="Services.ITriggerPullSource"/>
    /// (the host's ~60-line query-and-project adapter over its upstream data), the shared
    /// <see cref="Services.TriggerPullOptions"/> bound from the <c>SurveysIngest</c> config
    /// section, the timer-agnostic <see cref="Services.TriggerPullService"/>, and the
    /// always-on <see cref="Services.TriggerPullWorker"/> loop. Call once per source —
    /// options and the worker register once, sources accumulate. Requires
    /// <see cref="AddSurveysApiServices{TDbContext}(IServiceCollection, SurveyApiOptions, IMvcBuilder)"/>
    /// to have been called (the engine's ingest service and DbContext alias come from there).
    /// Serverless hosts wanting their own timer skip this and register
    /// <c>TriggerPullService</c> + their source directly.
    /// </summary>
    public static IServiceCollection AddSurveysTriggerPull<TSource>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TSource : class, Services.ITriggerPullSource
    {
        var pullOptions = new Services.TriggerPullOptions();
        configuration.GetSection(Services.TriggerPullOptions.SectionName).Bind(pullOptions);

        services.TryAddSingleton(pullOptions);
        services.AddScoped<Services.ITriggerPullSource, TSource>();
        services.TryAddScoped<Services.TriggerPullService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, Services.TriggerPullWorker>());

        return services;
    }
}
