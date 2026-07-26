using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Shared;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Startup sanity check for the deployment-supplied options that only misbehave in
/// production. These are the settings whose defaults are correct for a developer running
/// the sample and wrong for anyone serving real customers, so nothing surfaces them
/// until a customer is already affected.
///
/// Deliberately logs rather than throws: a bad link template shouldn't take a whole host
/// application down at boot. The send path refuses individually
/// (<see cref="TriggerSchedulerService"/>), so the failure mode is "surveys don't go out
/// and the log says why", not "a customer receives a dead link".
/// </summary>
public class SurveyOptionsStartupCheck : IHostedService
{
    private readonly SurveyApiOptions options;
    private readonly IHostEnvironment environment;
    private readonly ILogger<SurveyOptionsStartupCheck> logger;

    public SurveyOptionsStartupCheck(
        SurveyApiOptions options,
        IHostEnvironment environment,
        ILogger<SurveyOptionsStartupCheck> logger)
    {
        this.options = options;
        this.environment = environment;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment()) return Task.CompletedTask;

        if (!PublicSurveyUrl.IsDeployable(options.PublicSurveyUrlTemplate))
        {
            logger.LogError(
                "ADP.Surveys is misconfigured: {Problem} Survey links copied from the dashboard " +
                "will not work, and the scheduler will refuse to send. Set " +
                "SurveyApiOptions.PublicSurveyUrlTemplate to the deployed survey app, " +
                "e.g. https://<host>/s/{{publicId}}",
                PublicSurveyUrl.DescribeProblem(options.PublicSurveyUrlTemplate));
        }

        if (!options.EnableSurveysActionTreeAuthorization)
        {
            logger.LogWarning(
                "ADP.Surveys is running with EnableSurveysActionTreeAuthorization = false. " +
                "Every authenticated user of this host can read, edit and delete surveys and " +
                "read every response through the API — hiding the navigation link does not " +
                "restrict the endpoints. Enable it once the Surveys action tree is granted.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
