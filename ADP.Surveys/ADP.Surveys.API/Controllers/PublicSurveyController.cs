using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.Answers;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Responses;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Anonymous public endpoints consumed by the renderer and agent-assist iframe.
/// Per Decision #7, there is <b>no auth</b> on these — the instance's public GUID
/// is the capability. Rate-limiting / captcha is an orthogonal concern.
/// </summary>
[Route("SurveyInstances")]
[ApiController]
[AllowAnonymous]
public class PublicSurveyController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly SurveyApiOptions options;

    public PublicSurveyController(ShiftDbContext db, SurveyApiOptions options)
    {
        this.db = db;
        this.options = options;
    }

    /// <summary>
    /// Returns the fully-resolved <see cref="SurveyDto"/> for the pinned version of
    /// this instance, along with minimal instance metadata the renderer needs
    /// (customer reference pre-fill, meta blob). Safe to cache by the renderer
    /// since versions are immutable.
    /// </summary>
    [HttpGet("{publicId:guid}/schema")]
    public async Task<IActionResult> GetSchema([FromRoute] Guid publicId)
    {
        var instance = await db.Set<SurveyInstance>()
            .AsNoTracking()
            .Include(i => i.SurveyVersion)
            .FirstOrDefaultAsync(i => i.PublicID == publicId && !i.IsDeleted);

        if (instance is null) return NotFound();

        if (instance.Status == SurveyInstanceStatus.Expired)
            return StatusCode(StatusCodes.Status410Gone, new { Message = "This survey link has expired." });

        // No deployment branding configured → serve the raw schema JSON untouched so
        // the renderer hydrates against the exact bytes frozen at publish time.
        if (options.DefaultBranding is null)
            return Content(instance.SurveyVersion.ResolvedJson, "application/json");

        // Deployment branding overlay — applied at serve time (never baked into the
        // immutable version) so a rebrand reaches in-flight instances immediately.
        // The survey's own branding wins field-by-field.
        var resolved = JsonSerializer.Deserialize<SurveyDto>(instance.SurveyVersion.ResolvedJson, SurveySchemaSerializer.Options)!;
        resolved.Branding = BrandingDto.Merge(options.DefaultBranding, resolved.Branding);
        return Content(JsonSerializer.Serialize(resolved, SurveySchemaSerializer.Options), "application/json");
    }

    /// <summary>
    /// Accepts a submitted answer set for this instance. Validates against the pinned
    /// version's schema via <see cref="AnswerValidator"/> before persisting. Banked
    /// answers are keyed by <see cref="BankQuestion.BankEntryID"/> per Decision #11.
    /// </summary>
    [HttpPost("{publicId:guid}/responses")]
    public async Task<IActionResult> SubmitResponse(
        [FromRoute] Guid publicId,
        [FromBody] SurveyResponseSubmissionDto body)
    {
        if (body is null) return BadRequest(new { Message = "Missing body." });

        var instance = await db.Set<SurveyInstance>()
            .Include(i => i.SurveyVersion)
            .Include(i => i.Survey)
            .FirstOrDefaultAsync(i => i.PublicID == publicId && !i.IsDeleted);

        if (instance is null) return NotFound();

        if (instance.Status == SurveyInstanceStatus.Expired)
            return StatusCode(StatusCodes.Status410Gone, new { Message = "This survey link has expired." });

        if (instance.Status == SurveyInstanceStatus.Completed)
            return Conflict(new { Message = "This survey has already been completed." });

        // Pinned-version check: the caller's schemaVersion must match the instance's version.
        if (body.SchemaVersion != instance.SurveyVersion.Version)
            return BadRequest(new
            {
                Message = $"schemaVersion mismatch (submitted {body.SchemaVersion}, pinned {instance.SurveyVersion.Version}).",
            });

        SurveyDto resolved;
        try
        {
            resolved = JsonSerializer.Deserialize<SurveyDto>(
                instance.SurveyVersion.ResolvedJson, SurveySchemaSerializer.Options)
                ?? throw new InvalidOperationException("ResolvedJson deserialized to null.");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = $"Pinned version JSON is corrupt: {ex.Message}" });
        }

        var answerErrors = AnswerValidator.Validate(resolved, body.Answers);
        if (answerErrors.Count > 0)
            return BadRequest(new
            {
                Message = "Answer validation failed.",
                Errors = answerErrors.Select(e => new { e.QuestionId, e.Message }),
            });

        // Build a key → BankEntryID lookup for banked questions so each SurveyAnswer
        // stores the stable GUID anchor (Decision #11). Unknown keys fall back to null =
        // inline answer.
        var bankedKeys = resolved.Screens
            .OfType<Shared.DTOs.Screens.InlineScreenDto>()
            .SelectMany(s => s.Questions.Select(q => q.Inline?.Id).Where(id => !string.IsNullOrEmpty(id)))
            .ToHashSet()!;
        var bankMap = await db.Set<BankQuestion>().AsNoTracking()
            .Where(b => bankedKeys.Contains(b.Key) && !b.IsDeleted)
            .ToDictionaryAsync(b => b.Key, b => b.BankEntryID);

        var response = new SurveyResponse
        {
            SurveyInstanceID = instance.ID,
            StartedAt = body.Meta?.StartedAt,
            CompletedAt = body.Meta?.CompletedAt ?? DateTimeOffset.UtcNow,
            AgentId = body.Meta?.AgentId,
            Status = SurveyInstanceStatus.Completed,
        };
        db.Set<SurveyResponse>().Add(response);

        foreach (var (key, value) in body.Answers)
        {
            response.Answers.Add(new SurveyAnswer
            {
                KeyAtSubmission = key,
                BankEntryID = bankMap.TryGetValue(key, out var bid) ? bid : null,
                ValueJson = value.GetRawText(),
                Order = 0,
            });
        }

        instance.Status = SurveyInstanceStatus.Completed;
        // Submit cancels any pending sends. The scheduler's active-status filter would
        // already skip this row (Completed is terminal), but nulling NextSendAt + zeroing
        // RemindersRemaining makes the post-submit state explicit for admin queries and
        // reporting. Slice 5.
        instance.NextSendAt = null;
        instance.RemindersRemaining = 0;

        // Slice 7: write an outbox event in the same transaction. The dispatch worker
        // fans this out to registered ISurveyResponseSubscribers on its next tick.
        // The in-same-transaction write is what gives us at-least-once delivery: if
        // SaveChanges throws, neither the response nor the outbox event lands.
        var payload = new Shared.Triggers.SurveyOutboxPayload
        {
            SurveyId = instance.SurveyID,
            SurveyVersion = instance.SurveyVersion.Version,
            SurveyIntegrationId = instance.Survey.IntegrationId,
            InstancePublicId = instance.PublicID,
            TriggerId = instance.TriggerId,
            CustomerRef = instance.CustomerRef,
            RecipientAddress = instance.RecipientAddress,
            RecipientLocale = instance.RecipientLocale,
            EventType = "response-completed",
            CompletedAt = response.CompletedAt ?? DateTimeOffset.UtcNow,
            AgentId = response.AgentId,
            Answers = body.Answers,
            CandidateMetadataJson = instance.MetaDataJson,
        };

        db.Set<SurveyOutboxEvent>().Add(new SurveyOutboxEvent
        {
            SurveyResponse = response,
            SurveyInstance = instance,
            EventType = "response-completed",
            PayloadJson = JsonSerializer.Serialize(payload, SurveySchemaSerializer.Options),
            Status = SurveyOutboxEventStatus.Pending,
        });

        await db.SaveChangesAsync();

        return Ok(new { Message = "Submitted." });
    }

    /// <summary>
    /// Lightweight status check the renderer uses on resume — lets a returning user
    /// know whether their previous attempt has already completed / expired.
    /// </summary>
    [HttpGet("{publicId:guid}/status")]
    public async Task<IActionResult> GetStatus([FromRoute] Guid publicId)
    {
        var instance = await db.Set<SurveyInstance>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PublicID == publicId && !i.IsDeleted);

        if (instance is null) return NotFound();

        return Ok(new
        {
            Status = instance.Status.ToString(),
            SchemaVersion = instance.SurveyVersion.Version,
            instance.TriggeredAt,
        });
    }
}
