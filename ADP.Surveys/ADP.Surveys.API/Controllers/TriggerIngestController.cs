using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.API.Services;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Batch ingest endpoint for trigger candidate events. Callers are HTTP integrations
/// from outside the host — third-party systems, webhook adapters, operator tooling.
/// In-process callers (a scanner/puller inside the host) should inject
/// <see cref="TriggerIngestService"/> directly instead of going through HTTP.
/// Each item in the batch may produce 0..N <c>SurveyInstance</c> rows depending on
/// how many published triggers match.
///
/// Auth: <see cref="AuthorizeAttribute"/> unconditionally — this endpoint mass-creates
/// instances and must never be anonymous. The TypeAuth action gate
/// (<c>Operations.IngestTriggerEvents</c>) additionally applies when the host enables
/// action-tree authorization, so a dedicated service principal can be granted exactly
/// this and nothing else.
/// </summary>
[Route("Triggers")]
[ApiController]
[Authorize]
public class TriggerIngestController : ControllerBase
{
    private readonly TriggerIngestService service;
    private readonly SurveyApiOptions options;

    public TriggerIngestController(TriggerIngestService service, SurveyApiOptions options)
    {
        this.service = service;
        this.options = options;
    }

    [HttpPost("ingest")]
    public async Task<ActionResult<TriggerIngestResult>> Ingest(
        [FromBody] TriggerIngestRequest request,
        CancellationToken ct)
    {
        if (options.EnableSurveysActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(options.Actions.ResolvedIngestTriggerEvents))
                return Forbid();
        }

        if (request is null) return BadRequest(new { Message = "Missing body." });
        if (string.IsNullOrWhiteSpace(request.EventKind))
            return BadRequest(new { Message = "eventKind is required." });
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { Message = "items must contain at least one candidate." });

        var result = await service.IngestAsync(request, ct);
        return Ok(result);
    }
}
