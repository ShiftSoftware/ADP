using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Integrity;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

/// <summary>
/// The scan behind the publish gate. File questions collect metadata and discard the
/// bytes, so publishing one is blocked unless a deployment opts in — which only works if
/// the scan actually finds them, including the ones an author never typed themselves.
/// </summary>
public class FileQuestionScannerTests
{
    [Fact]
    public void Finds_FileQuestion_AndReportsItsPath()
    {
        var survey = SurveyWith(
            new TextQuestionDto { Id = "name" },
            new FileQuestionDto { Id = "licence-scan" });

        var paths = FileQuestionScanner.FindFileQuestionPaths(survey);

        var path = Assert.Single(paths);
        Assert.Contains("screens[0].questions[1]", path);
        Assert.Contains("licence-scan", path);
    }

    [Fact]
    public void SurveyWithoutFileQuestions_ScansClean()
    {
        var survey = SurveyWith(
            new TextQuestionDto { Id = "name" },
            new NpsQuestionDto { Id = "score" });

        Assert.Empty(FileQuestionScanner.FindFileQuestionPaths(survey));
    }

    [Fact]
    public void FindsEveryFileQuestion_AcrossScreens()
    {
        // Both must be reported: fixing only the first leaves the author publishing,
        // failing again, and fixing the next one.
        var survey = new SurveyDto
        {
            Screens =
            [
                Screen("one", new FileQuestionDto { Id = "front" }),
                Screen("two", new FileQuestionDto { Id = "back" }),
            ],
        };

        Assert.Equal(2, FileQuestionScanner.FindFileQuestionPaths(survey).Count);
    }

    [Fact]
    public void UnresolvedTemplateRefScreens_AreSkippedNotCrashed()
    {
        // The scan runs after SchemaResolver, so a ref here means something upstream is
        // wrong — the integrity validator reports that. This just must not throw.
        var survey = new SurveyDto
        {
            Screens = [new ScreenTemplateRefDto { TemplateRef = "customer-info" }],
        };

        Assert.Empty(FileQuestionScanner.FindFileQuestionPaths(survey));
    }

    private static SurveyDto SurveyWith(params QuestionDto[] questions) =>
        new() { Screens = [Screen("s1", questions)] };

    private static InlineScreenDto Screen(string id, params QuestionDto[] questions) => new()
    {
        Id = id,
        Questions = [.. questions.Select(QuestionEntryDto.FromInline)],
    };
}
