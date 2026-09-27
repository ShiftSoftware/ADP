using System.Text.Json;
using ShiftMapper;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core.Mapping;

namespace ShiftSoftware.ADP.Surveys.Data.Mappers;

// SM0017 is reported on the bank question's write map for the BankEntryID Condition: a conditioned map cannot be
// projected. A write map never is - it runs onto a tracked row - and the maps a repository projects are the list maps,
// which carry no Condition.
#pragma warning disable SM0017

/// <summary>
/// The ONE place the surveys module customizes its CRUD maps. Every repository triple's four maps (entity ↔ view,
/// entity → list, entity → entity) are declared automatically from the repository's type arguments, and most need
/// nothing. The pairs written here are the ones convention cannot do: JSON columns read into typed DTOs and written
/// back, comma-separated tags, list members derived from other rows, and server-owned members a request must not
/// write. Each <c>CreateMap</c> REPLACES the automatic map for that pair (SM0047, informational) and the other pairs of
/// the triple stay automatic. A repository says nothing about what a member maps from.
/// <para>
/// Two things a <c>CreateMap</c> does not inherit from the automatic map it replaces, so both are written: a write map
/// is declared as the REVERSE of the view map (a DTO is a subset of its entity, so the entity-only members it leaves
/// untouched are the quiet SM0006 rather than one SM0001 each), and flattening is off (<see cref="ConfigureDefaults"/>),
/// as it is on the automatic maps.
/// </para>
/// <para>
/// Where the maps run decides what they may contain. A repository PROJECTS the list maps (<c>ProjectTo</c>, one query
/// per page), and runs the view and write maps in memory on a loaded row. A method call in a list map is therefore
/// client-evaluated by EF in the final Select - legal, but the member cannot be sorted or filtered on in the query.
/// </para>
/// </summary>
public class SurveysMapper : ShiftMapperBase
{
    public SurveysMapper()
    {
        // The framework's rules (the members it owns such as ID, the hash-id and foreign-key conversions) reach a map
        // through the repository markers only in a project that closes ShiftRepository<,,,>. These maps are ALSO
        // generated into every project that references this assembly, most of which close none, so the class carries
        // the pack itself.
        AddConversions<ShiftEntityConversions>();

        AddSurveyMaps();
        AddSurveyInstanceMaps();
        AddBankQuestionMaps();
        AddScreenTemplateMaps();
    }

    /// <summary>
    /// The automatic maps these replace do not flatten; the maps written here keep that, so replacing one changes
    /// nothing but the members written.
    /// </summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Survey
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddSurveyMaps()
    {
        // The list map stays automatic: every member of SurveyListDTO is a plain column.

        CreateMap<Survey, SurveyAdminDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // The draft is a JSON column, so no convention can derive it. The view map runs in memory, which is what
            // makes the deserialize legal here.
            .ForMember(d => d.Draft, opt => opt.MapFrom(e => DeserializeDraft(e)))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            .ReverseMap()
            // Serialize through the canonical options so the wire format stays consistent with what the renderer and
            // SDK expect. Note the null case produces EMPTY STRING, not null - the column is non-nullable and the old
            // profile chose "" deliberately.
            .ForMember(e => e.DraftJson, opt => opt.MapFrom(dto =>
                dto.Draft == null ? "" : JsonSerializer.Serialize(dto.Draft, SurveySchemaSerializer.Options)))

            // TRAP 3-WRITE. PublishedVersionNumber is SERVER-owned: the publish flow derives it, and the old profile
            // ignored it on the reverse map for exactly this reason. Without this the convention would happily write
            // it from the request body, letting a client claim any published version number it likes through an
            // ordinary PUT.
            .ForMember(e => e.PublishedVersionNumber, opt => opt.Ignore());
    }

    /// <summary>
    /// Deserializes the draft JSON and stamps <see cref="SurveyDto.SurveyId"/> with the entity's long ID.
    ///
    /// <para>
    /// <b>The stamp is the part that matters.</b> This is not a plain deserialize: SurveyId is server-owned - never
    /// authored through the builder - and published snapshots carry whatever is on the Draft at publish time. Dropping
    /// the stamp would compile cleanly and simply return the field as null, which is why it is carried over verbatim
    /// rather than replaced by the convention.
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

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // SurveyInstance
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddSurveyInstanceMaps()
    {
        // LIST: every member here is part of ONE SQL projection, so all four must stay EF-translatable: a constant
        // comparison, a navigation hop, and two subqueries. Unlike the two JSON-derived members on BankQuestion and
        // ScreenTemplate, nothing here is a method call - and nothing here may become one.
        //
        // Status needs NOTHING: the enum converts to the DTO's int by convention. The view and write maps stay
        // automatic too - SurveyInstanceWriteMapperGoldenTests pins what the write map may touch.
        CreateMap<SurveyInstance, SurveyInstanceListDTO>()
            .ForMember(d => d.IsTest, opt => opt.MapFrom(e => e.TriggeredBy == SurveysConstants.DashboardTestTriggerSource))
            .ForMember(d => d.SchemaVersion, opt => opt.MapFrom(e => e.SurveyVersion.Version))

            // TRAP 1. The `!IsDeleted` predicate is the entire point of these two lines.
            //
            // Drop it and the count silently includes soft-deleted responses: the endpoint still returns 200, the body
            // still has the right shape, and the number is still plausible. No diagnostic fires, no test fails on
            // shape. The ONLY thing that catches it is a value diff against a baseline whose seed contains a
            // soft-deleted response - which the Surveys parity seed does, on instance 5200001, precisely so this line
            // is guarded.
            .ForMember(d => d.ResponseCount, opt => opt.MapFrom(e => e.Responses.Count(r => !r.IsDeleted)))

            // TRAP 1, same shape and the same reasoning: without the filter a soft-deleted response could supply the
            // Max and the instance would report a completion time that no live response accounts for.
            .ForMember(d => d.CompletedAt, opt => opt.MapFrom(e => e.Responses.Where(r => !r.IsDeleted).Max(r => r.CompletedAt)));
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // BankQuestion
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddBankQuestionMaps()
    {
        // LIST: SPIKE-3. Type is derived by PARSING A JSON COLUMN, and the list map is projected - so on the face of
        // it a method call here cannot survive translation.
        //
        // It does, and the reason is worth writing down: EF Core permits CLIENT EVALUATION in the final Select
        // projection, and the projection ends in exactly that. So the call is evaluated in memory per row, exactly as
        // it was under AutoMapper and the previous generated map.
        //
        // The limit this inherits, unchanged and PRE-EXISTING: EF does NOT allow client evaluation in query
        // operators, so `$orderby=Type` fails with "The LINQ expression ... could not be translated". That is not a
        // regression; it is the same behaviour, preserved.
        CreateMap<BankQuestion, BankQuestionListDTO>()
            .ForMember(d => d.Type, opt => opt.MapFrom(e => ExtractQuestionType(e.QuestionJson)));

        CreateMap<BankQuestion, BankQuestionAdminDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            .ForMember(d => d.Question, opt => opt.MapFrom(e =>
                JsonSerializer.Deserialize<QuestionDto>(e.QuestionJson, SurveySchemaSerializer.Options)))

            // Null/empty yields an EMPTY LIST, not null - see ReadTags.
            .ForMember(d => d.Tags, opt => opt.MapFrom(e => ReadTags(e.Tags)))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            .ReverseMap()
            // The explicit typeof(QuestionDto) overload is deliberate: QuestionDto is JSON-polymorphic over 14
            // concrete question types, and serializing through the STATIC type is what emits the "type"
            // discriminator. Serializing through the runtime type would silently drop it and every stored question
            // would fail to deserialize on the way back out.
            .ForMember(e => e.QuestionJson, opt => opt.MapFrom(dto =>
                dto.Question == null ? "" : JsonSerializer.Serialize(dto.Question, typeof(QuestionDto), SurveySchemaSerializer.Options)))

            // Reverse direction of the same asymmetry: null or EMPTY list yields NULL, not "".
            .ForMember(e => e.Tags, opt => opt.MapFrom(dto => WriteTags(dto.Tags)))

            // TRAP 3-WRITE. Locked is SERVER-owned - flipped true automatically on first publish reference - and it is
            // not merely a displayed flag: it arms BankQuestionRepository's immutability guards. Without this ignore a
            // client could unlock a locked bank question through an ordinary PUT body and then edit what the lock
            // exists to freeze.
            .ForMember(e => e.Locked, opt => opt.Ignore())

            // SPIKE-4. A CONDITIONAL write, not an ignore. BankEntryID is server-owned on create (the entity's
            // `= Guid.NewGuid()` default) but admin flows may legitimately carry it on update. An ignore would break
            // those updates; a plain write would overwrite the generated default with Guid.Empty on create. A
            // Condition that says no LEAVES THE MEMBER ALONE, which is what makes "keep the current value"
            // expressible.
            .ForMember(e => e.BankEntryID, opt => opt.Condition((dto, _, _) => dto.BankEntryID != Guid.Empty));
    }

    /// <summary>
    /// Reads the "type" discriminator out of the stored question JSON. See the list map for why a method call is legal
    /// there.
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

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ScreenTemplate
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddScreenTemplateMaps()
    {
        // LIST: the second SPIKE-3 instance, and it resolves identically to BankQuestion.Type: the count is derived
        // by parsing a JSON column, EF client-evaluates it in the final Select, and `$orderby=QuestionCount`
        // consequently fails to translate - pre-existing, not introduced here.
        CreateMap<ScreenTemplate, ScreenTemplateListDTO>()
            .ForMember(d => d.QuestionCount, opt => opt.MapFrom(e => CountTemplateQuestions(e.TemplateJson)));

        // NO Ignore on the write map. The old reverse map carried no Ignore() either, so there is no trap 3-write
        // here: the written member set matches the old reverse map exactly, with nothing extra.
        CreateMap<ScreenTemplate, ScreenTemplateAdminDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            .ForMember(d => d.Template, opt => opt.MapFrom(e =>
                JsonSerializer.Deserialize<ScreenTemplateDto>(e.TemplateJson, SurveySchemaSerializer.Options)))

            // Null/empty yields an EMPTY LIST, not null - see ReadTags.
            .ForMember(d => d.Tags, opt => opt.MapFrom(e => ReadTags(e.Tags)))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            .ReverseMap()
            // ScreenTemplateDto is NOT polymorphic, so unlike BankQuestion.QuestionJson this one needs no explicit
            // static-type overload. Null still yields EMPTY STRING, not null.
            .ForMember(e => e.TemplateJson, opt => opt.MapFrom(dto =>
                dto.Template == null ? "" : JsonSerializer.Serialize(dto.Template, SurveySchemaSerializer.Options)))

            .ForMember(e => e.Tags, opt => opt.MapFrom(dto => WriteTags(dto.Tags)));
    }

    /// <summary>Counts the questions array in the stored template JSON.</summary>
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

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Tags (BankQuestion and ScreenTemplate): a comma-separated column, a list on the DTO
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The comma-separated column as a list; a null or empty column yields an EMPTY LIST, never null.
    /// <para>
    /// WIRE-CONTRACT PARITY, and the parity harness is what found this. The AutoMapper profile these maps replaced
    /// split the column with a helper that returned NULL for a null/empty column - but AutoMapper's
    /// AllowNullCollections defaults to FALSE, so it silently coerced that null into an EMPTY LIST on the way out.
    /// Every response these endpoints have ever served therefore carried <c>"Tags": []</c>, never a missing member.
    /// Reproducing the helper faithfully would have shipped <c>Tags</c> as ABSENT - a wire-contract change invisible in
    /// the profile source, which is why it was only caught by diffing real response bodies. The coercion is written
    /// out because it is now OUR behaviour to own, not a framework default doing it behind us.
    /// </para>
    /// </summary>
    private static List<string> ReadTags(string? raw) =>
        string.IsNullOrEmpty(raw)
            ? new List<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>The list as the comma-separated column; a null or EMPTY list yields NULL, not "".</summary>
    private static string? WriteTags(List<string>? tags) =>
        tags == null || tags.Count == 0 ? null : string.Join(",", tags);
}
#pragma warning restore SM0017
