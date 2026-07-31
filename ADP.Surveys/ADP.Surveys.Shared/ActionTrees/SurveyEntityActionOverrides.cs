using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Surveys.Shared.ActionTrees;

/// <summary>
/// TypeAuth action overrides for the three CRUD surfaces the module exposes on both sides of
/// the wire — controllers on the API, list and form pages in Blazor. Each property, when set,
/// replaces the corresponding <see cref="SurveysActionTree"/> action as the gate on that
/// surface; null falls back to the module's own action.
/// </summary>
/// <remarks>
/// The point is to make authorization switchable for a host that already has an action
/// covering surveys in its own tree. Requiring them to adopt a second tree first is the
/// reason authorization stays off.
///
/// Each side configures its own instance (<c>SurveyApiOptions.Actions</c> and
/// <c>SurveysWebOptions.Actions</c>), so point both at the same actions: a UI gated more
/// loosely than the API hands the user buttons that 403, and one gated more tightly hides
/// work the user is allowed to do.
/// </remarks>
public class SurveyEntityActionOverrides
{
    /// <summary>Gate on the Survey CRUD surface. Default <see cref="SurveysActionTree.Surveys"/>.</summary>
    public ReadWriteDeleteAction? Surveys { get; set; }

    /// <summary>Gate on the Question Bank CRUD surface. Default <see cref="SurveysActionTree.BankQuestions"/>.</summary>
    public ReadWriteDeleteAction? BankQuestions { get; set; }

    /// <summary>Gate on the Screen Template CRUD surface. Default <see cref="SurveysActionTree.ScreenTemplates"/>.</summary>
    public ReadWriteDeleteAction? ScreenTemplates { get; set; }

    /// <summary><see cref="Surveys"/> or the module's own action.</summary>
    public ReadWriteDeleteAction ResolvedSurveys => Surveys ?? SurveysActionTree.Surveys;

    /// <summary><see cref="BankQuestions"/> or the module's own action.</summary>
    public ReadWriteDeleteAction ResolvedBankQuestions => BankQuestions ?? SurveysActionTree.BankQuestions;

    /// <summary><see cref="ScreenTemplates"/> or the module's own action.</summary>
    public ReadWriteDeleteAction ResolvedScreenTemplates => ScreenTemplates ?? SurveysActionTree.ScreenTemplates;
}
