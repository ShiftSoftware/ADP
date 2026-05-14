using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.API.Services;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Batch ingest endpoint for trigger candidate events. Upstream callers (in-process
/// Hangfire scanners, separate worker processes, webhook adapters, the manual-send
/// operator UI) all converge here. Each item in the batch may produce 0..N
/// <c>SurveyInstance</c> rows depending on how many published triggers match.
/// </summary>
[Route("Triggers")]
[ApiController]
public class TriggerIngestController : ControllerBase
{
    private readonly TriggerIngestService service;

    public TriggerIngestController(TriggerIngestService service)
    {
        this.service = service;
    }

    [HttpPost("ingest")]
    public async Task<ActionResult<TriggerIngestResult>> Ingest(
        [FromBody] TriggerIngestRequest request,
        CancellationToken ct)
    {
        if (request is null) return BadRequest(new { Message = "Missing body." });
        if (string.IsNullOrWhiteSpace(request.EventKind))
            return BadRequest(new { Message = "eventKind is required." });
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { Message = "items must contain at least one candidate." });

        var result = await service.IngestAsync(request, ct);
        return Ok(result);
    }
}
