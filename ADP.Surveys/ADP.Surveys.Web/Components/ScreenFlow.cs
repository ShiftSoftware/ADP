using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

namespace ShiftSoftware.ADP.Surveys.Web.Components;

/// <summary>
/// Static end-of-flow classification for builder affordances (rail badges,
/// inspector notes). Mirrors the renderer's implicit terminal rule
/// (<c>navigation.ts</c> / <c>computeNext</c>): a zero-question screen with no
/// <c>nextScreen</c> auto-submits on arrival and doubles as the thank-you
/// state; the last screen in array order with no onward path submits when the
/// respondent presses Next. Logic rules and banked question types are not
/// evaluated — this is an authoring hint, not the navigation engine.
/// </summary>
internal static class ScreenFlow
{
    public enum EndKind
    {
        None,

        /// <summary>Zero questions + no nextScreen — submits on arrival.</summary>
        AutoSubmit,

        /// <summary>Last screen in the flow with questions — the Next press submits.</summary>
        SubmitsOnNext,
    }

    public static EndKind Classify(SurveyDto? draft, int index)
    {
        if (draft is null || index < 0 || index >= draft.Screens.Count) return EndKind.None;
        if (draft.Screens[index] is not InlineScreenDto screen) return EndKind.None;
        if (!string.IsNullOrWhiteSpace(screen.NextScreen)) return EndKind.None;

        if (screen.Questions is not { Count: > 0 }) return EndKind.AutoSubmit;

        if (index != draft.Screens.Count - 1) return EndKind.None;

        // A terminal navigationList routes via its options — the renderer hides
        // the Next button entirely, so "Submits on Next" would mislead.
        if (screen.Questions[^1].Inline is NavigationListQuestionDto) return EndKind.None;

        return EndKind.SubmitsOnNext;
    }
}
