using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;

public sealed record SampleSurveyRecipe(
    string IntegrationId,
    string Name,
    SurveyDto Draft,
    IReadOnlyList<BankRecipe> Banks,
    IReadOnlyList<TemplateRecipe> Templates);

public sealed record BankRecipe(string Key, QuestionDto Question, string? BiColumn = null, string? Tags = null);

public sealed record TemplateRecipe(string Key, ScreenTemplateDto Template, string? Tags = null);
