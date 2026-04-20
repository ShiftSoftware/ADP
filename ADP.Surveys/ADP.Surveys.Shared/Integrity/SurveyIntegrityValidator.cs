using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

namespace ShiftSoftware.ADP.Surveys.Shared.Integrity;

/// <summary>
/// Runs cross-document integrity checks against a <see cref="SurveyDto"/> that has
/// already been through <see cref="Resolver.SchemaResolver"/> (no refs left).
///
/// Verifies:
/// <list type="bullet">
///   <item>Screen ids are unique within the survey.</item>
///   <item>Every inline-screen <c>NextScreen</c> resolves to a real screen id.</item>
///   <item>Every <c>navigationList</c> option's <c>NextScreen</c> resolves.</item>
///   <item>Every logic-rule <c>Then.Goto</c> resolves.</item>
///   <item>Question ids are unique within each screen.</item>
///   <item>No ref forms survive (sanity check — the resolver should have caught this).</item>
/// </list>
///
/// Does <b>not</b> check: expression syntax in logic rules (that's the LogicEvaluator's
/// job), or that answers match questions (that's <c>AnswerValidator</c>'s job).
/// </summary>
public static class SurveyIntegrityValidator
{
    public static IReadOnlyList<IntegrityError> Validate(SurveyDto resolved)
    {
        var errors = new List<IntegrityError>();
        var screenIds = CollectScreenIds(resolved, errors);

        for (var i = 0; i < resolved.Screens.Count; i++)
        {
            var path = $"screens[{i}]";
            var screen = resolved.Screens[i];

            if (screen is ScreenTemplateRefDto)
            {
                errors.Add(new IntegrityError($"{path}.templateRef",
                    "Unresolved ScreenTemplateRef in a supposedly-resolved survey — run SchemaResolver first."));
                continue;
            }

            if (screen is not InlineScreenDto inline) continue;

            CheckInlineScreen(inline, path, screenIds, errors);
        }

        CheckLogicGotos(resolved, screenIds, errors);

        return errors;
    }

    private static HashSet<string> CollectScreenIds(SurveyDto resolved, List<IntegrityError> errors)
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < resolved.Screens.Count; i++)
        {
            if (resolved.Screens[i] is not InlineScreenDto inline) continue;
            if (string.IsNullOrEmpty(inline.Id)) continue;
            if (!seen.Add(inline.Id))
                errors.Add(new IntegrityError($"screens[{i}].id",
                    $"Duplicate screen id '{inline.Id}'. Screen ids must be unique within a survey."));
        }
        return seen;
    }

    private static void CheckInlineScreen(InlineScreenDto screen, string path, HashSet<string> screenIds, List<IntegrityError> errors)
    {
        if (!string.IsNullOrEmpty(screen.NextScreen) && !screenIds.Contains(screen.NextScreen))
            errors.Add(new IntegrityError($"{path}.nextScreen",
                $"nextScreen '{screen.NextScreen}' does not match any screen id in this survey."));

        var questionIds = new HashSet<string>();
        for (var j = 0; j < screen.Questions.Count; j++)
        {
            var qPath = $"{path}.questions[{j}]";
            var entry = screen.Questions[j];

            if (entry.Ref is not null)
            {
                errors.Add(new IntegrityError($"{qPath}.bankRef",
                    "Unresolved QuestionRef in a supposedly-resolved survey — run SchemaResolver first."));
                continue;
            }

            if (entry.Inline is null) continue;
            var question = entry.Inline;

            if (!string.IsNullOrEmpty(question.Id) && !questionIds.Add(question.Id))
                errors.Add(new IntegrityError($"{qPath}.id",
                    $"Duplicate question id '{question.Id}' within screen '{screen.Id}'."));

            if (question is NavigationListQuestionDto nav)
                CheckNavigationOptions(nav, qPath, screenIds, errors);
        }
    }

    private static void CheckNavigationOptions(NavigationListQuestionDto nav, string path, HashSet<string> screenIds, List<IntegrityError> errors)
    {
        for (var i = 0; i < nav.Options.Count; i++)
        {
            var opt = nav.Options[i];
            if (!string.IsNullOrEmpty(opt.NextScreen) && !screenIds.Contains(opt.NextScreen))
                errors.Add(new IntegrityError($"{path}.options[{i}].nextScreen",
                    $"nextScreen '{opt.NextScreen}' does not match any screen id in this survey."));
        }
    }

    private static void CheckLogicGotos(SurveyDto survey, HashSet<string> screenIds, List<IntegrityError> errors)
    {
        for (var i = 0; i < survey.Logic.Count; i++)
        {
            var rule = survey.Logic[i];
            var target = rule.Then?.Goto;
            if (string.IsNullOrEmpty(target)) continue;
            if (!screenIds.Contains(target))
                errors.Add(new IntegrityError($"logic[{i}].then.goto",
                    $"Logic rule goto '{target}' does not match any screen id in this survey."));
        }
    }
}
