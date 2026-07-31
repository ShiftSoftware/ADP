using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Web.WebServices;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ADP.Surveys.Web.Extensions;

public static class SurveysWebExtensions
{
    /// <summary>
    /// Registers Surveys Blazor services into the consumer's DI container. The consumer
    /// is responsible for <c>AddShiftBlazor</c>, <c>AddShiftIdentity</c>, <c>AddTypeAuth</c>,
    /// and HttpClient registration. This method only wires Surveys' own options, action
    /// tree, assembly for routing discovery, and per-entity HTTP services. Mirrors
    /// <c>AddMenuBlazorServices</c> 1:1.
    /// </summary>
    public static IServiceCollection AddSurveysBlazorServices(
        this IServiceCollection services,
        Action<SurveysWebOptions>? configure = null)
    {
        var options = new SurveysWebOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Skipped when the host gates the pages on its own actions via SurveysWebOptions.Actions,
        // so the module's unused tree stays out of the permissions UI. Mirrors the API side.
        if (options.RegisterSurveysActionTree)
            services.Configure<TypeAuthBlazorOptions>(o => o.AddActionTree<SurveysActionTree>());

        // Lets ShiftBlazor's DefaultApp discover routes in this assembly.
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(SurveysWebExtensions).Assembly));

        services.AddScoped<SurveyService>();

        // Scoped so its id→label cache lives as long as the user's session rather than
        // being rebuilt for every answers dialog.
        services.AddScoped<SourcedOptionsResolver>();
        services.AddScoped<BankQuestionService>();
        services.AddScoped<ScreenTemplateService>();

        return services;
    }
}
