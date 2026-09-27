using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Data.Tests;

/// <summary>
/// The maps in <c>Mappers/SurveysMapper.cs</c>, run through the mapper a host resolves. Each rule there was a
/// repository's <c>UseGeneratedMapper</c> configuration before it became a <c>CreateMap</c>; the assertions are what the
/// previous generated maps did, so a map that stopped doing it shows up here. What
/// <see cref="FrameworkCompatibilityTests"/> already pins - the draft round trip, the ignored PublishedVersionNumber and
/// Locked, the kept BankEntryID, the empty tags and the soft-delete-aware response count - is not repeated.
/// </summary>
public class SurveysMapperTests
{
    private static IEnumerable<(Type Entity, Type List, Type View)> RepositoryTriples() =>
        typeof(Marker).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.BaseType)
            .Where(b => b is { IsGenericType: true } && b.GetGenericTypeDefinition() == typeof(ShiftRepository<,,,>))
            .Select(b => b!.GetGenericArguments())
            .Select(a => (a[1], a[2], a[3]));

    /// <summary>A repository resolves its mapper only when all four of its maps exist.</summary>
    [Fact]
    public void Every_repository_triple_is_mapped_in_all_four_directions()
    {
        var mapper = ModuleMapper.Mapper();

        var triples = RepositoryTriples().ToList();
        Assert.NotEmpty(triples);

        foreach (var (entity, list, view) in triples)
        {
            Assert.True(mapper.CanMap(entity, view), $"{entity.Name} -> {view.Name}");
            Assert.True(mapper.CanMap(view, entity), $"{view.Name} -> {entity.Name}");
            Assert.True(mapper.CanMap(entity, list), $"{entity.Name} -> {list.Name}");
            Assert.True(mapper.CanMap(entity, entity), $"{entity.Name} -> {entity.Name}");
        }
    }

    [Fact]
    public void Startup_mapping_validation_passes() =>
        ShiftEntityMapperValidation.Validate(ModuleMapper.Services(), [typeof(Marker).Assembly]);

    // ───── Survey ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_survey_list_row_carries_its_columns()
    {
        var mapper = ModuleMapper.For<Survey, SurveyListDTO, SurveyAdminDTO>();
        var survey = new Survey { Name = "Survey", IntegrationId = "INT-1", PublishedVersionNumber = 2 };

        var row = mapper.MapToList(new[] { survey }.AsQueryable()).Single();

        Assert.Equal("Survey", row.Name);
        Assert.Equal("INT-1", row.IntegrationId);
        Assert.Equal(2, row.PublishedVersionNumber);
    }

    [Fact]
    public void A_survey_without_a_draft_views_none_and_saves_an_empty_string()
    {
        var mapper = ModuleMapper.For<Survey, SurveyListDTO, SurveyAdminDTO>();
        var survey = new Survey { Name = "Survey", DraftJson = "" };

        Assert.Null(mapper.MapToView(survey).Draft);

        survey.DraftJson = "{}";
        mapper.MapToEntity(new SurveyAdminDTO { Name = "Survey", Draft = null }, survey);

        Assert.Equal("", survey.DraftJson);
    }

    // ───── SurveyInstance ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_response_row_carries_its_status_as_a_number_and_counts_no_deleted_response()
    {
        var mapper = ModuleMapper.For<SurveyInstance, SurveyInstanceListDTO, SurveyInstanceAdminDTO>();
        var triggeredAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var instance = new SurveyInstance
        {
            PublicID = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SurveyVersion = new SurveyVersion { Version = 5 },
            TriggeredAt = triggeredAt,
            TriggeredBy = "crm",
            TriggerId = "T-1",
            Channel = "email",
            RecipientAddress = "someone@example.invalid",
            Status = SurveyInstanceStatus.Opened,
            NextSendAt = triggeredAt.AddDays(2),
            RemindersRemaining = 2,
            Responses = [new SurveyResponse { IsDeleted = true, CompletedAt = triggeredAt.AddHours(1) }],
        };

        var row = mapper.MapToList(new[] { instance }.AsQueryable()).Single();

        Assert.Equal((int)SurveyInstanceStatus.Opened, row.Status);
        Assert.Equal(instance.PublicID, row.PublicID);
        Assert.Equal(triggeredAt, row.TriggeredAt);
        Assert.Equal("crm", row.TriggeredBy);
        Assert.Equal("T-1", row.TriggerId);
        Assert.Equal("email", row.Channel);
        Assert.Equal("someone@example.invalid", row.RecipientAddress);
        Assert.Equal(triggeredAt.AddDays(2), row.NextSendAt);
        Assert.Null(row.LastSentAt);
        Assert.Equal(2, row.RemindersRemaining);
        Assert.Equal(5, row.SchemaVersion);

        // Only the dashboard's own trigger source is a test, and a deleted response neither counts nor completes.
        Assert.False(row.IsTest);
        Assert.Equal(0, row.ResponseCount);
        Assert.Null(row.CompletedAt);
    }

    // ───── BankQuestion ───────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("{\"type\":\"rating\",\"id\":\"q\"}", QuestionType.Rating)]
    [InlineData("{\"id\":\"q\",\"type\":\"singleChoice\"}", QuestionType.SingleChoice)]
    [InlineData("{\"id\":\"q\"}", QuestionType.Text)]
    [InlineData("not json", QuestionType.Text)]
    [InlineData("", QuestionType.Text)]
    public void A_bank_question_list_row_reads_its_type_from_the_stored_json(string questionJson, QuestionType expected)
    {
        var mapper = ModuleMapper.For<BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>();
        var question = new BankQuestion { Key = "feedback", QuestionJson = questionJson, Locked = true, Retired = true };

        var row = mapper.MapToList(new[] { question }.AsQueryable()).Single();

        Assert.Equal(expected, row.Type);
        Assert.Equal(question.BankEntryID, row.BankEntryID);
        Assert.Equal("feedback", row.Key);
        Assert.True(row.Locked);
        Assert.True(row.Retired);
    }

    [Fact]
    public void A_saved_question_keeps_its_type_discriminator()
    {
        var mapper = ModuleMapper.For<BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>();
        var question = new BankQuestion { Key = "feedback" };

        mapper.MapToEntity(new BankQuestionAdminDTO { Key = "feedback", Question = new RatingQuestionDto { Max = 7 } }, question);

        Assert.Equal(QuestionType.Rating, mapper.MapToList(new[] { question }.AsQueryable()).Single().Type);
        Assert.Equal(7, Assert.IsType<RatingQuestionDto>(mapper.MapToView(question).Question).Max);
    }

    [Fact]
    public void A_bank_question_takes_a_given_bank_entry_id_and_saves_no_question_as_an_empty_string()
    {
        var mapper = ModuleMapper.For<BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>();
        var question = new BankQuestion { Key = "feedback", QuestionJson = "{\"type\":\"text\",\"id\":\"q\"}" };
        var given = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        mapper.MapToEntity(new BankQuestionAdminDTO { Key = "feedback", BankEntryID = given, Question = null }, question);

        Assert.Equal(given, question.BankEntryID);
        Assert.Equal("", question.QuestionJson);
    }

    [Fact]
    public void Bank_question_tags_read_split_and_trimmed_and_save_joined()
    {
        var mapper = ModuleMapper.For<BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>();
        var question = new BankQuestion { Key = "feedback", QuestionJson = "{\"type\":\"text\",\"id\":\"q\"}", Tags = " nps , csat ,, " };

        Assert.Equal(["nps", "csat"], mapper.MapToView(question).Tags!);

        mapper.MapToEntity(new BankQuestionAdminDTO { Key = "feedback", Question = new TextQuestionDto(), Tags = ["a", "b"] }, question);
        Assert.Equal("a,b", question.Tags);

        mapper.MapToEntity(new BankQuestionAdminDTO { Key = "feedback", Question = new TextQuestionDto(), Tags = null }, question);
        Assert.Null(question.Tags);
    }

    // ───── ScreenTemplate ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("{\"id\":\"t\",\"questions\":[{},{},{}]}", 3)]
    [InlineData("{\"questions\":[]}", 0)]
    [InlineData("{\"questions\":{}}", 0)]
    [InlineData("{\"id\":\"t\"}", 0)]
    [InlineData("not json", 0)]
    [InlineData("", 0)]
    public void A_screen_template_list_row_counts_the_stored_questions(string templateJson, int expected)
    {
        var mapper = ModuleMapper.For<ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO>();
        var template = new ScreenTemplate { Key = "welcome", TemplateJson = templateJson };

        var row = mapper.MapToList(new[] { template }.AsQueryable()).Single();

        Assert.Equal(expected, row.QuestionCount);
        Assert.Equal("welcome", row.Key);
    }

    [Fact]
    public void Screen_template_tags_round_trip_and_no_template_saves_as_an_empty_string()
    {
        var mapper = ModuleMapper.For<ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO>();
        var template = new ScreenTemplate { Key = "welcome", TemplateJson = "{\"id\":\"t\",\"questions\":[]}", Tags = "a, b" };

        var view = mapper.MapToView(template);
        Assert.Equal("t", view.Template!.Id);
        Assert.Equal(["a", "b"], view.Tags!);

        mapper.MapToEntity(new ScreenTemplateAdminDTO { Key = "closing", Template = null, Tags = ["x", "y"] }, template);

        Assert.Equal("closing", template.Key);
        Assert.Equal("", template.TemplateJson);
        Assert.Equal("x,y", template.Tags);
    }
}
