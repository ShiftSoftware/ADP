using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

// SHENGEN008 fires on `Template` and is a FALSE POSITIVE here. The check pairs a view member with
// an entity member of the SAME NAME; this DTO member is a strongly-typed ScreenTemplateDto that is written
// back to the DIFFERENTLY NAMED `TemplateJson` column (serialized), which the generator cannot see
// into. Verified against the emitted code before suppressing, per conventions.md section 7:
// MapToEntityGenerated contains `existing.TemplateJson = this.__ShiftMap.InvokeEntity<string>(dto,
// existing, context, "TemplateJson", ...)`, so the member demonstrably does NOT "silently fail to
// save" - it saves through the JSON column, exactly as the AutoMapper profile this replaced did.
#pragma warning disable SHENGEN008
public class ScreenTemplateRepository : ShiftRepository<ShiftDbContext, ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO>
{
    public ScreenTemplateRepository(ShiftDbContext db) : base(db, x => x.UseGeneratedMapper(map => map

        // ── LIST ──────────────────────────────────────────────────────────────────────────
        // The second SPIKE-3 instance, and it resolves identically to BankQuestion.Type: the count
        // is derived by parsing a JSON column, EF client-evaluates it in the final Select, and
        // `$orderby=QuestionCount` consequently fails to translate - pre-existing, not introduced
        // here. See BankQuestionRepository for the full reasoning.
        .ForList(d => d.QuestionCount, e => CountTemplateQuestions(e.TemplateJson))

        // ── VIEW ──────────────────────────────────────────────────────────────────────────
        .ForView(d => d.Template, e =>
            JsonSerializer.Deserialize<ScreenTemplateDto>(e.TemplateJson, SurveySchemaSerializer.Options))

        // Null/empty yields NULL, not an empty list.
        // WIRE-CONTRACT PARITY, and the harness is what found this.
        //
        // SplitTags returns NULL for a null/empty column - and the old profile called exactly the
        // same helper. But AutoMapper's AllowNullCollections defaults to FALSE, so it silently
        // coerced that null into an EMPTY LIST on the way out. Every response this endpoint has
        // ever served therefore carried `"Tags": []`, never a missing member.
        //
        // Reproducing the helper faithfully would have shipped `Tags` as ABSENT - a wire-contract
        // change invisible in the profile source, which is why it was only caught by diffing real
        // response bodies (verification.md Rule 5 keeps [] and null distinct precisely for this).
        // The coercion is restated explicitly here because it is now OUR behaviour to own, not a
        // framework default doing it behind us.
        .ForView(d => d.Tags, e => SplitTags(e.Tags) ?? new List<string>())

        // ── ENTITY ────────────────────────────────────────────────────────────────────────
        // ScreenTemplateDto is NOT polymorphic, so unlike BankQuestion.QuestionJson this one needs
        // no explicit static-type overload. Null still yields EMPTY STRING, not null.
        .ForEntity(e => e.TemplateJson, dto =>
            dto.Template == null ? "" : JsonSerializer.Serialize(dto.Template, SurveySchemaSerializer.Options))

        .ForEntity(e => e.Tags, dto =>
            dto.Tags == null || dto.Tags.Count == 0 ? null : string.Join(",", dto.Tags))))

        // NO IgnoreEntity on this triple. The old reverse map carried no Ignore() either, so there
        // is no trap 3-write here - item I verifies from the emitted MapToEntityGenerated that the
        // written member set matches the old reverse map exactly, with nothing extra.
    {
    }

    public override async ValueTask<ScreenTemplate> UpsertAsync(
        ScreenTemplate entity, ScreenTemplateAdminDTO dto, ActionTypes actionType, long? userId,
        Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (dto.Template is null)
            throw new ShiftEntityException(new("Invalid",
                "Template is required. Paste a ScreenTemplateDto JSON payload in the editor."));

        var keyExists = await db.Set<ScreenTemplate>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Key == dto.Key)
            .Where(x => x.ID != entity.ID)
            .AnyAsync();

        if (keyExists)
            throw new ShiftEntityException(new("Duplicate", $"A screen template with key '{dto.Key}' already exists."));

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    /// <summary>Counts the questions array in the stored template JSON. Carried over verbatim.</summary>
    private static int CountTemplateQuestions(string templateJson)
    {
        if (string.IsNullOrEmpty(templateJson)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(templateJson);
            if (doc.RootElement.TryGetProperty("questions", out var q) && q.ValueKind == JsonValueKind.Array)
                return q.GetArrayLength();
        }
        catch { /* fall through */ }
        return 0;
    }

    /// <summary>
    /// Comma-separated column to list. Null or empty input yields <c>null</c>, never an empty list.
    /// </summary>
    private static List<string>? SplitTags(string? raw) =>
        string.IsNullOrEmpty(raw)
            ? null
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
#pragma warning restore SHENGEN008
