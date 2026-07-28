using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.API.Services;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Admin/test surface for the trigger scheduler + outbox dispatch. Production wiring
/// (a periodic BackgroundService or Hangfire job) is a slice 4 follow-up — this
/// controller's tick endpoint exists today so the e2e harness can drive both passes
/// deterministically and operators can force a poll out-of-band if needed.
///
/// One tick orchestrates three passes:
///   1. Send-due rows (scheduler service)
///   2. Expire stale rows (scheduler service)
///   3. Drain outbox to subscribers (outbox service)
/// The services are independent and can be invoked separately at the service layer
/// if a future deployment wants different cadences for each.
///
/// Auth: <see cref="AuthorizeAttribute"/> unconditionally — a tick triggers real channel
/// sends and outbox dispatches, so it must never be anonymous. The TypeAuth action gate
/// (<c>Operations.RunScheduler</c>) additionally applies when the host enables
/// action-tree authorization, so a timer/service principal can be granted exactly this.
/// </summary>
[Route("Triggers/scheduler")]
[ApiController]
[Authorize]
public class TriggerSchedulerController : ControllerBase
{
    private readonly TriggerSchedulerService scheduler;
    private readonly OutboxDispatchService outbox;
    private readonly SurveyApiOptions options;

    public TriggerSchedulerController(
        TriggerSchedulerService scheduler, OutboxDispatchService outbox, SurveyApiOptions options)
    {
        this.scheduler = scheduler;
        this.outbox = outbox;
        this.options = options;
    }

    [HttpPost("tick")]
    public async Task<IActionResult> Tick([FromQuery] int batchSize = 100, CancellationToken ct = default)
    {
        if (options.EnableSurveysActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(options.Actions.ResolvedRunScheduler))
                return Forbid();
        }

        if (batchSize < 1 || batchSize > 1000)
            return BadRequest(new { Message = "batchSize must be between 1 and 1000." });

        var schedulerResult = await scheduler.PollOnceAsync(batchSize, ct);
        var outboxResult = await outbox.PollOnceAsync(batchSize, ct);

        return Ok(new
        {
            schedulerResult.Processed,
            schedulerResult.Expired,
            outboxResult.Dispatched,
            // Failed = will be retried on a later tick; DeadLettered = gave up. Alerting
            // belongs on the second one — a non-zero Failed is often just a blip mid-recovery.
            OutboxFailed = outboxResult.Failed,
            OutboxDeadLettered = outboxResult.DeadLettered,
        });
    }
}
