using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Surveys.Shared.ActionTrees;

[ActionTree("Surveys", "Surveys Module Permissions")]
public class SurveysActionTree
{
    public readonly static ReadWriteDeleteAction Surveys = new("Surveys");
    public readonly static ReadWriteDeleteAction BankQuestions = new("Bank Questions");
    public readonly static ReadWriteDeleteAction ScreenTemplates = new("Screen Templates");

    [ActionTree("Survey Operations", "Special Survey Operations")]
    public class Operations
    {
        public readonly static BooleanAction PublishSurvey = new("Publish Survey");
        public readonly static BooleanAction ViewResponses = new("View Survey Responses");
        public readonly static BooleanAction ExportResponses = new("Export Responses To Excel");
    }
}
