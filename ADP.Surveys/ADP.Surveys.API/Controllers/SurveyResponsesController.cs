using System.Text;
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
    private readonly Services.SurveyResponseExporter exporter;

    public SurveyResponsesController(
        ShiftDbContext db,
        IHashIdService hashIdService,
        SurveyApiOptions options,
        Services.SurveyResponseExporter exporter)
    {
        this.db = db;
        this.hashIdService = hashIdService;
        this.options = options;
        this.exporter = exporter;
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
        if (Forbidden(options.Actions.ResolvedViewResponses, out var forbid)) return forbid!;
        var template = options.PublicSurveyUrlTemplate;
        return Ok(new PublicUrlTemplateDTO
        {
            Template = string.IsNullOrWhiteSpace(template) ? null : template,
            Warning = PublicSurveyUrl.IsDeployable(template) ? null : PublicSurveyUrl.DescribeProblem(template),
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
        if (Forbidden(options.Actions.ResolvedViewResponses, out var forbid)) return forbid!;

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
        if (Forbidden(options.Actions.ResolvedCreateTestInstances, out var forbid)) return forbid!;
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

    /// <summary>
    /// Wide CSV of every submitted response for this survey — one row per response, one
    /// column per answer key, anchored on the current bank key so a corrected key re-labels
    /// history rather than splitting a question in two.
    /// </summary>
    /// <remarks>
    /// Test instances are excluded by default: they are authoring noise, and quietly
    /// including them skews any ratio computed from the export. Pass
    /// <c>includeTests=true</c> to get them, which is occasionally what you want when
    /// debugging a survey rather than reporting on it.
    /// </remarks>
    [HttpGet("{surveyId}/export")]
    public async Task<IActionResult> Export(
        [FromRoute] string surveyId,
        [FromQuery] bool includeTests = false,
        CancellationToken ct = default)
    {
        if (Forbidden(options.Actions.ResolvedExportResponses, out var forbid)) return forbid!;
        if (!TryDecodeSurveyId(surveyId, out var id, out var error)) return error!;

        var survey = await db.Set<Survey>().FirstOrDefaultAsync(s => s.ID == id && !s.IsDeleted, ct);
        if (survey is null) return NotFound();

        var csv = await exporter.BuildCsvAsync(id, includeTests, ct);

        // UTF-8 BOM: without it Excel reads the file as the system codepage and mangles
        // every non-ASCII label — which for a Russian- or Arabic-authored survey is
        // all of them.
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"{Slug(survey.Name)}-responses-{DateTimeOffset.UtcNow:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>Filename-safe survey name — the raw name can contain anything an author typed.</summary>
    private static string Slug(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "survey";
        var cleaned = new string(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
        cleaned = string.Join('-', cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Length == 0 ? "survey" : cleaned[..Math.Min(cleaned.Length, 60)];
    }

    private string? ComposePublicUrl(Guid publicId) =>
        PublicSurveyUrl.Compose(options.PublicSurveyUrlTemplate, publicId);

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
