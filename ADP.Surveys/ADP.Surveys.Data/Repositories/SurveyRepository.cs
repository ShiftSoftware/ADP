using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

// SHENGEN008 fires on `Draft` and is a FALSE POSITIVE here. The check pairs a view member with
// an entity member of the SAME NAME; this DTO member is a strongly-typed SurveyDto that is written
// back to the DIFFERENTLY NAMED `DraftJson` column (serialized), which the generator cannot see
// into. Verified against the emitted code before suppressing, per conventions.md section 7:
// MapToEntityGenerated contains `existing.DraftJson = this.__ShiftMap.InvokeEntity<string>(dto,
// existing, context, "DraftJson", ...)`, so the member demonstrably does NOT "silently fail to
// save" - it saves through the JSON column, exactly as the AutoMapper profile this replaced did.
#pragma warning disable SHENGEN008
public class SurveyRepository : ShiftRepository<ShiftDbContext, Survey, SurveyListDTO, SurveyAdminDTO>
{
    // SurveyListDTO needs nothing: every one of its members is a plain column the convention maps.
    // The old profile's bare CreateMap<Survey, SurveyListDTO>() carried no ForMember at all, so
    // there is nothing to restate here (conventions.md section 3 - delete rather than restate).
    public SurveyRepository(ShiftDbContext db) : base(db, x => x.UseGeneratedMapper(map => map

        // ── VIEW ──────────────────────────────────────────────────────────────────────────
        // The draft is a JSON column, so no convention can derive it. ForView takes a Func and
        // runs in memory, which is what makes the deserialize legal here.
        .ForView(d => d.Draft, e => DeserializeDraft(e))

        // ── ENTITY ────────────────────────────────────────────────────────────────────────
        // Serialize through the canonical options so the wire format stays consistent with what
        // the renderer and SDK expect. Note the null case produces EMPTY STRING, not null - the
        // column is non-nullable and the old profile chose "" deliberately.
        .ForEntity(e => e.DraftJson, dto =>
            dto.Draft == null ? "" : JsonSerializer.Serialize(dto.Draft, SurveySchemaSerializer.Options))

        // TRAP 3-WRITE. PublishedVersionNumber is SERVER-owned: the publish flow derives it, and
        // the old profile ignored it on the reverse map for exactly this reason. Without this the
        // convention would happily write it from the request body, letting a client claim any
        // published version number it likes through an ordinary PUT.
        .IgnoreEntity(e => e.PublishedVersionNumber)))
    {
    }

    /// <summary>
    /// Deserializes the draft JSON and stamps <see cref="SurveyDto.SurveyId"/> with the entity's
    /// long ID.
    ///
    /// <para>
    /// <b>The stamp is the part that matters.</b> This is not a plain deserialize: SurveyId is
    /// server-owned - never authored through the builder - and published snapshots carry whatever
    /// is on the Draft at publish time. Dropping the stamp would compile cleanly and simply return
    /// the field as null, which is why it is carried over verbatim rather than replaced by the
    /// convention.
    /// </para>
    /// </summary>
    private static SurveyDto? DeserializeDraft(Survey entity)
    {
        if (string.IsNullOrEmpty(entity.DraftJson))
            return null;

        var dto = JsonSerializer.Deserialize<SurveyDto>(entity.DraftJson, SurveySchemaSerializer.Options);
        if (dto is not null)
            dto.SurveyId = entity.ID.ToString();
        return dto;
    }
}
#pragma warning restore SHENGEN008
