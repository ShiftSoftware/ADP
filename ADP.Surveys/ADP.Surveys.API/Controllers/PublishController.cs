using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Bank;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.Integrity;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Resolver;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Runs the publish pipeline end-to-end: load draft → FluentValidation → resolve refs
/// via <see cref="SchemaResolver"/> + <see cref="EfBankSource"/> → cross-document
/// <see cref="SurveyIntegrityValidator"/> → SHA-256 hash → skip-if-unchanged → write
/// a new immutable <see cref="SurveyVersion"/> → flip <c>Locked</c> on every touched
/// bank question per Decision #11.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class PublishController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly EfBankSource bankSource;
    private readonly IValidator<SurveyDto> surveyValidator;
    private readonly SurveyApiOptions options;

    public PublishController(
        ShiftDbContext db,
        EfBankSource bankSource,
        IValidator<SurveyDto> surveyValidator,
        SurveyApiOptions options)
    {
        this.db = db;
        this.bankSource = bankSource;
        this.surveyValidator = surveyValidator;
        this.options = options;
    }

    /// <summary>
    /// Publishes the survey's current draft. Returns the new version number (or the
    /// existing one if the hash matches and nothing needed to change).
    /// </summary>
    [HttpPost("{id}")]
    public async Task<IActionResult> Publish([FromRoute] string id)
    {
        if (options.EnableSurveysActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(SurveysActionTree.Operations.PublishSurvey))
                return Forbid();
        }

        long surveyId;
        try { surveyId = ShiftEntityHashIdService.Decode<SurveyListDTO>(id); }
        catch
        {
            return BadRequest(new { Message = "Invalid survey id." });
        }

        var survey = await db.Set<Survey>().FirstOrDefaultAsync(s => s.ID == surveyId && !s.IsDeleted);
        if (survey is null) return NotFound();

        if (string.IsNullOrEmpty(survey.DraftJson))
            return BadRequest(new { Message = "Survey has no draft to publish." });

        // 1. Deserialize + FluentValidation
        SurveyDto draft;
        try
        {
            draft = JsonSerializer.Deserialize<SurveyDto>(survey.DraftJson, SurveySchemaSerializer.Options)
                ?? throw new InvalidOperationException("Draft deserialized to null.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Draft JSON is not a valid SurveyDto: {ex.Message}" });
        }

        var fluentResult = await surveyValidator.ValidateAsync(draft);
        if (!fluentResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Draft failed validation.",
                Errors = fluentResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        // 2. Resolve refs
        var resolve = SchemaResolver.Resolve(draft, bankSource);
        if (!resolve.Success)
        {
            return BadRequest(new
            {
                Message = "Draft has unresolved bank or template references.",
                Errors = resolve.Errors.Select(e => new { e.Path, e.Message })
            });
        }

        // 3. Integrity (cross-document refs)
        var integrityErrors = SurveyIntegrityValidator.Validate(resolve.Survey!);
        if (integrityErrors.Count > 0)
        {
            return BadRequest(new
            {
                Message = "Resolved schema failed integrity checks.",
                Errors = integrityErrors.Select(e => new { e.Path, e.Message })
            });
        }

        // 4. Serialize + hash
        var nextVersionNumber = (survey.PublishedVersionNumber ?? 0) + 1;
        resolve.Survey!.SurveyId = survey.ID.ToString();
        resolve.Survey.Version = nextVersionNumber;
        resolve.Survey.PublishedAt = DateTimeOffset.UtcNow;

        var resolvedJson = JsonSerializer.Serialize(resolve.Survey, SurveySchemaSerializer.Options);
        var hash = Sha256Hex(resolvedJson);

        // 5. Skip if nothing changed since the last publish.
        var latest = await db.Set<SurveyVersion>()
            .Where(v => v.SurveyID == survey.ID)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();
        if (latest is not null && latest.Hash == hash)
        {
            return Ok(new
            {
                Message = "No schema changes since last publish — version unchanged.",
                Version = latest.Version,
                Hash = latest.Hash,
            });
        }

        // 6. Write new version + update pointer
        var version = new SurveyVersion
        {
            SurveyID = survey.ID,
            Version = nextVersionNumber,
            PublishedAt = resolve.Survey.PublishedAt.Value,
            ResolvedJson = resolvedJson,
            Hash = hash,
        };
        db.Set<SurveyVersion>().Add(version);
        survey.PublishedVersionNumber = nextVersionNumber;

        // 7. Lock bank entries this publish depends on — Decision #11 append-only guarantee.
        var usedKeys = bankSource.UsedQuestionIds.ToArray();
        if (usedKeys.Length > 0)
        {
            var toLock = await db.Set<BankQuestion>()
                .Where(b => usedKeys.Contains(b.Key) && !b.Locked && !b.IsDeleted)
                .ToListAsync();
            foreach (var b in toLock) b.Locked = true;
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Published.",
            Version = version.Version,
            Hash = version.Hash,
            LockedBankEntries = usedKeys,
        });
    }

    private static string Sha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
