using System.Text.Json;
using AutoMapper;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Data.AutoMapperProfiles;

/// <summary>
/// Entity ↔ admin-DTO mappings. The bulk of the work is bridging between the SQL
/// columns (JSON strings, comma-separated tag lists) and the strongly-typed schema
/// DTOs the admin API surface carries. All JSON goes through the canonical
/// <see cref="SurveySchemaSerializer.Options"/> so the wire format stays consistent
/// with what the renderer / SDK expect.
/// </summary>
public class GeneralMappingProfile : Profile
{
    public GeneralMappingProfile()
    {
        MapSurvey();
        MapBankQuestion();
        MapScreenTemplate();
    }

    private void MapSurvey()
    {
        CreateMap<Survey, SurveyListDTO>();

        CreateMap<Survey, SurveyAdminDTO>()
            .ForMember(d => d.Draft, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.DraftJson)
                    ? null
                    : JsonSerializer.Deserialize<SurveyDto>(src.DraftJson, SurveySchemaSerializer.Options)))
            .ReverseMap()
            .ForMember(e => e.DraftJson, opt => opt.MapFrom(src =>
                src.Draft == null ? "" : JsonSerializer.Serialize(src.Draft, SurveySchemaSerializer.Options)))
            // PublishedVersionNumber is server-owned; ignore incoming writes.
            .ForMember(e => e.PublishedVersionNumber, opt => opt.Ignore());
    }

    private void MapBankQuestion()
    {
        CreateMap<BankQuestion, BankQuestionListDTO>()
            .ForMember(d => d.Type, opt => opt.MapFrom(src => ExtractQuestionType(src.QuestionJson)));

        CreateMap<BankQuestion, BankQuestionAdminDTO>()
            .ForMember(d => d.Question, opt => opt.MapFrom(src =>
                JsonSerializer.Deserialize<QuestionDto>(src.QuestionJson, SurveySchemaSerializer.Options)))
            .ForMember(d => d.Tags, opt => opt.MapFrom(src => SplitTags(src.Tags)))
            .ReverseMap()
            .ForMember(e => e.QuestionJson, opt => opt.MapFrom(src =>
                src.Question == null ? "" : JsonSerializer.Serialize(src.Question, typeof(QuestionDto), SurveySchemaSerializer.Options)))
            .ForMember(e => e.Tags, opt => opt.MapFrom(src =>
                src.Tags == null || src.Tags.Count == 0 ? null : string.Join(",", src.Tags)))
            // Locked is server-owned — flipped true automatically on first publish reference.
            .ForMember(e => e.Locked, opt => opt.Ignore())
            // BankEntryID is server-owned on create (entity's `= Guid.NewGuid()` default).
            // Skip mapping when the client sends Guid.Empty so the default survives; still
            // allow updates from authenticated admin flows that explicitly carry the value.
            .ForMember(e => e.BankEntryID, opt => opt.Condition(src => src.BankEntryID != Guid.Empty));
    }

    private void MapScreenTemplate()
    {
        CreateMap<ScreenTemplate, ScreenTemplateListDTO>()
            .ForMember(d => d.QuestionCount, opt => opt.MapFrom(src => CountTemplateQuestions(src.TemplateJson)));

        CreateMap<ScreenTemplate, ScreenTemplateAdminDTO>()
            .ForMember(d => d.Template, opt => opt.MapFrom(src =>
                JsonSerializer.Deserialize<ScreenTemplateDto>(src.TemplateJson, SurveySchemaSerializer.Options)))
            .ForMember(d => d.Tags, opt => opt.MapFrom(src => SplitTags(src.Tags)))
            .ReverseMap()
            .ForMember(e => e.TemplateJson, opt => opt.MapFrom(src =>
                src.Template == null ? "" : JsonSerializer.Serialize(src.Template, SurveySchemaSerializer.Options)))
            .ForMember(e => e.Tags, opt => opt.MapFrom(src =>
                src.Tags == null || src.Tags.Count == 0 ? null : string.Join(",", src.Tags)));
    }

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

    private static List<string>? SplitTags(string? raw) =>
        string.IsNullOrEmpty(raw)
            ? null
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
