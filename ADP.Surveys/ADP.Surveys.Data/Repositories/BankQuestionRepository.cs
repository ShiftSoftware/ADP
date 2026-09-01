using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

// SHENGEN008 fires on `Question` and is a FALSE POSITIVE here. The check pairs a view member with
// an entity member of the SAME NAME; this DTO member is a strongly-typed QuestionDto that is written
// back to the DIFFERENTLY NAMED `QuestionJson` column (serialized), which the generator cannot see
// into. Verified against the emitted code before suppressing, per conventions.md section 7:
// MapToEntityGenerated contains `existing.QuestionJson = this.__ShiftMap.InvokeEntity<string>(dto,
// existing, context, "QuestionJson", ...)`, so the member demonstrably does NOT "silently fail to
// save" - it saves through the JSON column, exactly as the AutoMapper profile this replaced did.
#pragma warning disable SHENGEN008
public class BankQuestionRepository : ShiftRepository<ShiftDbContext, BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>
{
    public BankQuestionRepository(ShiftDbContext db) : base(db, x => x.UseGeneratedMapper(map => map

        // ── LIST ──────────────────────────────────────────────────────────────────────────
        // SPIKE-3. Type is derived by PARSING A JSON COLUMN, and ForList is spliced into the SQL
        // projection - so on the face of it a method call here cannot survive translation.
        //
        // It does, and the reason is worth writing down: EF Core permits CLIENT EVALUATION in the
        // final Select projection, and both the old AutoMapper path and the generated
        // MapToListGenerated end in exactly that (Queryable.Select(queryable, projection)). So the
        // call is evaluated in memory per row, exactly as it was before.
        //
        // The limit this inherits, unchanged and PRE-EXISTING: EF does NOT allow client evaluation
        // in query operators, so `$orderby=Type` fails with "The LINQ expression ... could not be
        // translated" - verified against the pre-migration tree before this rewrite. That is not a
        // regression introduced here; it is the same behaviour, preserved.
        .ForList(d => d.Type, e => ExtractQuestionType(e.QuestionJson))

        // ── VIEW ──────────────────────────────────────────────────────────────────────────
        .ForView(d => d.Question, e =>
            JsonSerializer.Deserialize<QuestionDto>(e.QuestionJson, SurveySchemaSerializer.Options))

        // Null/empty input yields NULL, not an empty list. Rule 5 keeps null and [] distinct in the
        // parity diff, so getting this backwards would show up as a wire-contract change.
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
        // The explicit typeof(QuestionDto) overload is deliberate: QuestionDto is JSON-polymorphic
        // over 14 concrete question types, and serializing through the STATIC type is what emits
        // the "type" discriminator. Serializing through the runtime type would silently drop it and
        // every stored question would fail to deserialize on the way back out.
        .ForEntity(e => e.QuestionJson, dto =>
            dto.Question == null
                ? ""
                : JsonSerializer.Serialize(dto.Question, typeof(QuestionDto), SurveySchemaSerializer.Options))

        // Reverse direction of the same asymmetry: null or EMPTY list yields NULL, not "".
        .ForEntity(e => e.Tags, dto =>
            dto.Tags == null || dto.Tags.Count == 0 ? null : string.Join(",", dto.Tags))

        // TRAP 3-WRITE. Locked is SERVER-owned - flipped true automatically on first publish
        // reference - and it is not merely a displayed flag: it arms this repository's own
        // immutability guards below. Without this ignore a client could unlock a locked bank
        // question through an ordinary PUT body and then edit what the lock exists to freeze.
        .IgnoreEntity(e => e.Locked)

        // SPIKE-4. A CONDITIONAL write, not an ignore. BankEntryID is server-owned on create (the
        // entity's `= Guid.NewGuid()` default) but admin flows may legitimately carry it on update.
        // IgnoreEntity would break those updates; a plain ForEntity would overwrite the generated
        // default with Guid.Empty on create. The existing-aware overload is the only one that can
        // see the CURRENT entity value, which is what makes "leave it alone" expressible.
        //
        // Registered unconditionally with the condition INSIDE the delegate - that shape is what
        // keeps SHENGEN005 ("conditional mapper configuration") from firing.
        .ForEntity(e => e.BankEntryID, (dto, entity, ctx) =>
            dto.BankEntryID != Guid.Empty ? dto.BankEntryID : entity.BankEntryID)))
    {
    }

    public override async ValueTask<BankQuestion> UpsertAsync(
        BankQuestion entity, BankQuestionAdminDTO dto, ActionTypes actionType, long? userId,
        Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        // Admin DTO's Question is nullable so the JSON-editor form can surface parse
        // errors inline; this is the hard server-side backstop.
        if (dto.Question is null)
            throw new ShiftEntityException(new("Invalid",
                "Question is required. Paste a QuestionDto JSON payload in the editor."));

        // Uniqueness on human-readable Key (among non-deleted entries).
        var keyExists = await db.Set<BankQuestion>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Key == dto.Key)
            .Where(x => x.ID != entity.ID)
            .AnyAsync();

        if (keyExists)
            throw new ShiftEntityException(new("Duplicate", $"A bank question with key '{dto.Key}' already exists."));

        // Decision #9 lock enforcement. Once any published survey has referenced this
        // entry (Locked == true), the type + validation are frozen — only presentation
        // and Key (for typo correction) may change.
        //
        // These guards read `entity.*`, i.e. the entity as it stands BEFORE base.UpsertAsync maps
        // the DTO onto it. That ordering is load-bearing and unchanged by the migration: the
        // generated mapper, like the profile before it, runs inside base. If a future change ever
        // moved mapping ahead of this block, the guards would compare the client's value against
        // itself and pass vacuously.
        if (actionType == ActionTypes.Update && entity.Locked)
        {
            if (entity.BankEntryID != dto.BankEntryID)
                throw new ShiftEntityException(new("Locked",
                    "BankEntryID is immutable. It is the stable BI join anchor per Decision #11."));

            // Compare existing QuestionJson vs incoming Question to detect disallowed changes.
            // Conservative approach for this pass: simply block any type change; deeper
            // presentation-vs-semantic diffing can be added once we have a real builder UX.
            if (dto.Question is not null)
            {
                var incomingType = dto.Question.GetType();
                var existingDto = JsonSerializer.Deserialize<QuestionDto>(
                    entity.QuestionJson, SurveySchemaSerializer.Options);
                if (existingDto is not null && existingDto.GetType() != incomingType)
                    throw new ShiftEntityException(new("Locked",
                        $"Cannot change the question type of a locked bank entry (was {existingDto.GetType().Name}, got {incomingType.Name})."));
            }
        }

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    /// <summary>
    /// Reads the "type" discriminator out of the stored question JSON. Carried over verbatim from
    /// the deleted profile - see the ForList comment above for why a method call is legal here.
    /// </summary>
    private static QuestionType ExtractQuestionType(string questionJson)
    {
        if (string.IsNullOrEmpty(questionJson)) return QuestionType.Text;
        try
        {
            using var doc = JsonDocument.Parse(questionJson);
            if (doc.RootElement.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
            {
                // The discriminator matches the enum's [JsonStringEnumMemberName]. Delegate
                // parsing to System.Text.Json so it honors the configured naming.
                return JsonSerializer.Deserialize<QuestionType>($"\"{t.GetString()}\"", SurveySchemaSerializer.Options);
            }
        }
        catch { /* fall through */ }
        return QuestionType.Text;
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
