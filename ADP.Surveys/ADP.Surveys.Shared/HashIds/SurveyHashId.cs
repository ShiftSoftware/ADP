using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Surveys.Shared.HashIds;

public class SurveyHashId : JsonHashIdConverterAttribute<SurveyHashId>
{
    public SurveyHashId() : base(5) { }
}

public class SurveyVersionHashId : JsonHashIdConverterAttribute<SurveyVersionHashId>
{
    public SurveyVersionHashId() : base(5) { }
}

public class BankQuestionHashId : JsonHashIdConverterAttribute<BankQuestionHashId>
{
    public BankQuestionHashId() : base(5) { }
}

public class ScreenTemplateHashId : JsonHashIdConverterAttribute<ScreenTemplateHashId>
{
    public ScreenTemplateHashId() : base(5) { }
}

public class SurveyInstanceHashId : JsonHashIdConverterAttribute<SurveyInstanceHashId>
{
    public SurveyInstanceHashId() : base(5) { }
}

public class SurveyResponseHashId : JsonHashIdConverterAttribute<SurveyResponseHashId>
{
    public SurveyResponseHashId() : base(5) { }
}
