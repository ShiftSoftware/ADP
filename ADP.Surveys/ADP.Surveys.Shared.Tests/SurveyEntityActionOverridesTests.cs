using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.TypeAuth.Core.Actions;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class SurveyEntityActionOverridesTests
{
    [Fact]
    public void Unset_FallsBackToModuleTree()
    {
        var overrides = new SurveyEntityActionOverrides();

        Assert.Same(SurveysActionTree.Surveys, overrides.ResolvedSurveys);
        Assert.Same(SurveysActionTree.BankQuestions, overrides.ResolvedBankQuestions);
        Assert.Same(SurveysActionTree.ScreenTemplates, overrides.ResolvedScreenTemplates);
    }

    [Fact]
    public void Set_ReplacesModuleAction()
    {
        var hostAction = new ReadWriteDeleteAction("Host Surveys");
        var overrides = new SurveyEntityActionOverrides { Surveys = hostAction };

        Assert.Same(hostAction, overrides.ResolvedSurveys);
    }

    [Fact]
    public void Set_LeavesTheOtherSurfacesAlone()
    {
        var overrides = new SurveyEntityActionOverrides
        {
            Surveys = new ReadWriteDeleteAction("Host Surveys"),
        };

        // Overriding one surface must not drag the others off the module's tree — a host
        // gating surveys on its own action can still leave the bank on the module's.
        Assert.Same(SurveysActionTree.BankQuestions, overrides.ResolvedBankQuestions);
        Assert.Same(SurveysActionTree.ScreenTemplates, overrides.ResolvedScreenTemplates);
    }
}
