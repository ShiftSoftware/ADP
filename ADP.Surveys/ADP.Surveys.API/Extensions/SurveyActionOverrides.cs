using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Surveys.API.Extensions;

/// <summary>
/// Per-surface TypeAuth action overrides. Each property, when set, replaces the
/// corresponding <see cref="SurveysActionTree"/> action as the gate on that surface;
/// null falls back to the module's own action.
///
/// The point is to make <see cref="SurveyApiOptions.EnableSurveysActionTreeAuthorization"/>
/// switchable for a host that already has an action covering surveys in its own tree.
/// Requiring them to adopt a second tree first is the reason authorization stays off.
/// </summary>
public class SurveyActionOverrides
{
    /// <summary>Gate on the Survey CRUD surface. Default <see cref="SurveysActionTree.Surveys"/>.</summary>
    public ReadWriteDeleteAction? Surveys { get; set; }

    /// <summary>Gate on the Question Bank CRUD surface. Default <see cref="SurveysActionTree.BankQuestions"/>.</summary>
    public ReadWriteDeleteAction? BankQuestions { get; set; }

    /// <summary>Gate on the Screen Template CRUD surface. Default <see cref="SurveysActionTree.ScreenTemplates"/>.</summary>
    public ReadWriteDeleteAction? ScreenTemplates { get; set; }

    /// <summary>Gate on publishing a survey version. Default <c>Operations.PublishSurvey</c>.</summary>
    public BooleanAction? PublishSurvey { get; set; }

    /// <summary>Gate on reading instances and their answers. Default <c>Operations.ViewResponses</c>.</summary>
    public BooleanAction? ViewResponses { get; set; }

    /// <summary>Gate on exporting responses. Default <c>Operations.ExportResponses</c>.</summary>
    public BooleanAction? ExportResponses { get; set; }

    /// <summary>Gate on creating dashboard test instances. Default <c>Operations.CreateTestInstances</c>.</summary>
    public BooleanAction? CreateTestInstances { get; set; }

    /// <summary>Gate on the trigger ingest endpoint. Default <c>Operations.IngestTriggerEvents</c>.</summary>
    public BooleanAction? IngestTriggerEvents { get; set; }

    /// <summary>Gate on the scheduler/outbox tick endpoint. Default <c>Operations.RunScheduler</c>.</summary>
    public BooleanAction? RunScheduler { get; set; }

    internal ReadWriteDeleteAction ResolvedSurveys => Surveys ?? SurveysActionTree.Surveys;
    internal ReadWriteDeleteAction ResolvedBankQuestions => BankQuestions ?? SurveysActionTree.BankQuestions;
    internal ReadWriteDeleteAction ResolvedScreenTemplates => ScreenTemplates ?? SurveysActionTree.ScreenTemplates;
    internal BooleanAction ResolvedPublishSurvey => PublishSurvey ?? SurveysActionTree.Operations.PublishSurvey;
    internal BooleanAction ResolvedViewResponses => ViewResponses ?? SurveysActionTree.Operations.ViewResponses;
    internal BooleanAction ResolvedExportResponses => ExportResponses ?? SurveysActionTree.Operations.ExportResponses;
    internal BooleanAction ResolvedCreateTestInstances => CreateTestInstances ?? SurveysActionTree.Operations.CreateTestInstances;
    internal BooleanAction ResolvedIngestTriggerEvents => IngestTriggerEvents ?? SurveysActionTree.Operations.IngestTriggerEvents;
    internal BooleanAction ResolvedRunScheduler => RunScheduler ?? SurveysActionTree.Operations.RunScheduler;
}
