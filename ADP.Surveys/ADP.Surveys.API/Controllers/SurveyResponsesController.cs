using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Responses;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Authenticated read/test surface for survey instances and their recorded answers —
/// what the dashboard's per-survey Responses page talks to. Deliberately not a
/// ShiftEntity CRUD controller: instances are created by triggers (or the test-run
/// action here) and answered through the public endpoints; the dashboard only ever
/// lists and inspects them.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class SurveyResponsesController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly IHashIdService hashIdService;
    private readonly SurveyApiOptions options;

    public SurveyResponsesController(ShiftDbContext db, IHashIdService hashIdService, SurveyApiOptions options)
    {
        this.db = db;
        this.hashIdService = hashIdService;
        this.options = options;
    }

    /// <summary>
    /// The deployment's <c>PublicSurveyUrlTemplate</c> (with a <c>{publicId}</c>
    /// placeholder), so the dashboard can compose recipient links client-side —
    /// the instance LIST is served by the OData <c>SurveyInstanceController</c>,
    /// whose ProjectTo pipeline can't reach runtime options. Null when the
    /// deployment hasn't configured a public renderer URL.
    /// </summary>
    [HttpGet("public-url-template")]
    public IActionResult GetPublicUrlTemplate()
    {
        if (Forbidden(SurveysActionTree.Operations.ViewResponses, out var forbid)) return forbid!;
        return Ok(new PublicUrlTemplateDTO
        {
            Template = string.IsNullOrWhiteSpace(options.PublicSurveyUrlTemplate) ? null : options.PublicSurveyUrlTemplate,
        });
    }

    /// <summary>
    /// Full detail for one instance: metadata, every recorded response with its answers,
    /// and the pinned version's resolved schema JSON so the client can label answers
    /// exactly as the respondent saw them.
    /// </summary>
    [HttpGet("instance/{publicId:guid}")]
    public async Task<IActionResult> GetInstanceDetail([FromRoute] Guid publicId)
    {
        if (Forbidden(SurveysActionTree.Operations.ViewResponses, out var forbid)) return forbid!;

        var instance = await db.Set<SurveyInstance>().AsNoTracking()
            .Include(i => i.SurveyVersion)
            .FirstOrDefaultAsync(i => i.PublicID == publicId && !i.IsDeleted);
        if (instance is null) return NotFound();

        var responses = await db.Set<SurveyResponse>().AsNoTracking()
            .Where(r => r.SurveyInstanceID == instance.ID && !r.IsDeleted)
            .OrderBy(r => r.ID)
            .Select(r => new
            {
                r.StartedAt,
                r.CompletedAt,
                r.AgentId,
                Answers = r.Answers
                    .OrderBy(a => a.ID)
                    .Select(a => new { a.KeyAtSubmission, a.BankEntryID, a.ValueJson })
                    .ToList(),
            })
            .ToListAsync();

        return Ok(new SurveyInstanceDetailDTO
        {
            Instance = new SurveyInstanceSummaryDTO
            {
                PublicId = instance.PublicID,
                Status = instance.Status.ToString(),
                TriggeredAt = instance.TriggeredAt,
                TriggeredBy = instance.TriggeredBy,
                IsTest = instance.TriggeredBy == SurveysConstants.DashboardTestTriggerSource,
                Channel = instance.Channel,
                RecipientAddress = instance.RecipientAddress,
                RecipientLocale = instance.RecipientLocale,
                CustomerRef = instance.CustomerRef,
                SchemaVersion = instance.SurveyVersion.Version,
                ResponseCount = responses.Count,
                CompletedAt = responses.Max(r => r.CompletedAt),
                PublicUrl = ComposePublicUrl(instance.PublicID),
            },
            ResolvedJson = instance.SurveyVersion.ResolvedJson,
            Responses = responses.Select(r => new SurveyResponseItemDTO
            {
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                AgentId = r.AgentId,
                Answers = r.Answers.Select(a => new SurveyAnswerItemDTO
                {
                    Key = a.KeyAtSubmission,
                    BankEntryId = a.BankEntryID,
                    ValueJson = a.ValueJson,
                }).ToList(),
            }).ToList(),
        });
    }

    /// <summary>
    /// Creates a test instance pinned to the survey's latest published version.
    /// Marked <see cref="SurveysConstants.DashboardTestTriggerSource"/> so its
    /// responses never fan out through the outbox, and never scheduled for channel
    /// sends (<c>NextSendAt</c> stays null).
    /// </summary>
    [HttpPost("{surveyId}/test-instances")]
    public async Task<IActionResult> CreateTestInstance([FromRoute] string surveyId)
    {
        if (Forbidden(SurveysActionTree.Operations.CreateTestInstances, out var forbid)) return forbid!;
        if (!TryDecodeSurveyId(surveyId, out var id, out var bad)) return bad!;

        var survey = await db.Set<Survey>()
            .FirstOrDefaultAsync(s => s.ID == id && !s.IsDeleted);
        if (survey is null) return NotFound();

        var version = await db.Set<SurveyVersion>()
            .Where(v => v.SurveyID == id)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();
        if (version is null)
            return BadRequest(new { Message = "Survey has no published version — publish it before creating a test instance." });

        var instance = new SurveyInstance
        {
            SurveyID = survey.ID,
            SurveyVersionID = version.ID,
            TriggeredAt = DateTimeOffset.UtcNow,
            TriggeredBy = SurveysConstants.DashboardTestTriggerSource,
            Status = SurveyInstanceStatus.Pending,
            NextSendAt = null,
            RemindersRemaining = 0,
        };
        db.Set<SurveyInstance>().Add(instance);
        await db.SaveChangesAsync();

        return Ok(new CreateTestInstanceResultDTO
        {
            PublicId = instance.PublicID,
            SchemaVersion = version.Version,
            PublicUrl = ComposePublicUrl(instance.PublicID),
        });
    }

    private string? ComposePublicUrl(Guid publicId) =>
        string.IsNullOrWhiteSpace(options.PublicSurveyUrlTemplate)
            ? null
            : options.PublicSurveyUrlTemplate.Replace("{publicId}", publicId.ToString());

    private bool TryDecodeSurveyId(string surveyId, out long id, out IActionResult? error)
    {
        try
        {
            id = hashIdService.Decode<SurveyListDTO>(surveyId);
            error = null;
            return true;
        }
        catch
        {
            id = 0;
            error = BadRequest(new { Message = "Invalid survey id." });
            return false;
        }
    }

    private bool Forbidden(BooleanAction action, out IActionResult? result)
    {
        if (options.EnableSurveysActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(action))
            {
                result = Forbid();
                return true;
            }
        }
        result = null;
        return false;
    }
}
