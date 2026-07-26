using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.Integrity;

/// <summary>
/// Finds <c>file</c> questions in a resolved survey.
///
/// The platform does not upload files yet: the renderer records the selected file's
/// <c>{name, size, type}</c> into the answer map and the bytes are discarded. That is a
/// reasonable state for a not-yet-built feature and a terrible one to discover in
/// production — the survey looks like it collected a document, the response row says a
/// document was attached, and the document does not exist anywhere.
///
/// So publishing a survey containing a file question is blocked unless the deployment has
/// explicitly declared file handling (<c>SurveyApiOptions.FileUploadsSupported</c>). The
/// scan runs against the RESOLVED schema so banked and templated file questions are caught
/// too — an author can reach one without ever seeing it in their own draft.
/// </summary>
public static class FileQuestionScanner
{
    /// <summary>
    /// Returns the dotted path of every <c>file</c> question in the resolved survey.
    /// Empty when there are none.
    /// </summary>
    public static IReadOnlyList<string> FindFileQuestionPaths(SurveyDto resolved)
    {
        var paths = new List<string>();

        for (var s = 0; s < resolved.Screens.Count; s++)
        {
            if (resolved.Screens[s] is not InlineScreenDto screen) continue;

            for (var q = 0; q < screen.Questions.Count; q++)
            {
                var question = screen.Questions[q].Inline;
                if (question?.QuestionType != QuestionType.File) continue;

                var id = string.IsNullOrEmpty(question.Id) ? "?" : question.Id;
                paths.Add($"screens[{s}].questions[{q}] (id '{id}')");
            }
        }

        return paths;
    }
}
