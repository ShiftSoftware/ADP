using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.API.Services;

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
/// </summary>
[Route("Triggers/scheduler")]
[ApiController]
public class TriggerSchedulerController : ControllerBase
{
    private readonly TriggerSchedulerService scheduler;
    private readonly OutboxDispatchService outbox;

    public TriggerSchedulerController(TriggerSchedulerService scheduler, OutboxDispatchService outbox)
    {
        this.scheduler = scheduler;
        this.outbox = outbox;
    }

    [HttpPost("tick")]
    public async Task<IActionResult> Tick([FromQuery] int batchSize = 100, CancellationToken ct = default)
    {
        if (batchSize < 1 || batchSize > 1000)
            return BadRequest(new { Message = "batchSize must be between 1 and 1000." });

        var schedulerResult = await scheduler.PollOnceAsync(batchSize, ct);
        var outboxResult = await outbox.PollOnceAsync(batchSize, ct);

        return Ok(new
        {
            schedulerResult.Processed,
            schedulerResult.Expired,
            outboxResult.Dispatched,
            OutboxFailed = outboxResult.Failed,
        });
    }
}
