using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
/// is the capability. Rate-limiting / captcha is an orthogonal concern (provider
/// interface will land in Phase 5).
/// </summary>
[Route("SurveyInstances")]
[ApiController]
[AllowAnonymous]
public class PublicSurveyController : ControllerBase
{
    private readonly ShiftDbContext db;

    public PublicSurveyController(ShiftDbContext db)
    {
        this.db = db;
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

        // Returns the raw schema JSON untouched so the renderer hydrates against the
        // exact bytes that were frozen at publish time.
        return Content(instance.SurveyVersion.ResolvedJson, "application/json");
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
