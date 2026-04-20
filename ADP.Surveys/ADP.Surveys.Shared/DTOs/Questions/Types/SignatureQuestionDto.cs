using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class SignatureQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Signature;
}

public class SignatureQuestionDtoValidator : QuestionDtoBaseValidator<SignatureQuestionDto>
{
    public SignatureQuestionDtoValidator() { }
}
