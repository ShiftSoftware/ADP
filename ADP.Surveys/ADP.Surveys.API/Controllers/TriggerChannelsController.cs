using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.API.Channels;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Exposes the channel registry to the builder UI so trigger authoring can
/// surface a dropdown of currently-registered <c>ISurveyChannel</c> keys.
/// Authors can still type any key — registry contents are environment-specific
/// and a survey may reference a channel that isn't wired in this process.
/// </summary>
[Route("Triggers")]
[ApiController]
public class TriggerChannelsController : ControllerBase
{
    private readonly SurveyChannelRegistry registry;

    public TriggerChannelsController(SurveyChannelRegistry registry)
    {
        this.registry = registry;
    }

    [HttpGet("channels")]
    public ActionResult<IEnumerable<string>> List()
        => Ok(registry.RegisteredKeys.OrderBy(k => k, StringComparer.Ordinal));
}
