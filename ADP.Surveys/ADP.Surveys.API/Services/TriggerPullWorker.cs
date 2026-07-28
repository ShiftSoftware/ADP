using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Always-on loop over <see cref="TriggerPullService.ScanOnceAsync"/> for hosts that run
/// as long-lived processes. Each tick creates a DI scope and scans every registered
/// <see cref="ITriggerPullSource"/> in turn (each source keeps its own cursor row, so
/// sources fail and drain independently). Serverless hosts skip this worker and call
/// <c>ScanOnceAsync</c> from their own timer trigger instead.
/// </summary>
public class TriggerPullWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TriggerPullOptions options;
    private readonly ILogger<TriggerPullWorker> logger;

    public TriggerPullWorker(
        IServiceScopeFactory scopeFactory,
        TriggerPullOptions options,
        ILogger<TriggerPullWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Trigger pull worker registered but {Section}:Enabled is false — idle.", TriggerPullOptions.SectionName);
            return;
        }

        logger.LogInformation(
            "Trigger pull worker starting: interval={Interval}m, lookback={Lookback}d, maxRows={MaxRows}",
            options.ScanIntervalMinutes, options.LookbackDays, options.MaxRowsPerScan);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.StartupDelaySeconds), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.ScanIntervalMinutes));
            do
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TriggerPullService>();

                foreach (var source in scope.ServiceProvider.GetServices<ITriggerPullSource>())
                {
                    try
                    {
                        await service.ScanOnceAsync(source, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // A failed scan is not fatal: the cursor didn't move, so the next
                        // tick retries exactly where this one left off.
                        logger.LogError(ex, "Trigger pull scan failed for [{EventKind}]; will retry on the next interval", source.EventKind);
                    }
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}
