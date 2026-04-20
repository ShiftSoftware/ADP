using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class FileQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.File;

    [JsonPropertyName("acceptedTypes")]
    public List<string>? AcceptedTypes { get; set; }

    [JsonPropertyName("maxSizeBytes")]
    public long? MaxSizeBytes { get; set; }
}

public class FileQuestionDtoValidator : QuestionDtoBaseValidator<FileQuestionDto>
{
    public FileQuestionDtoValidator()
    {
        RuleFor(x => x.MaxSizeBytes).GreaterThan(0).When(x => x.MaxSizeBytes.HasValue);
        RuleForEach(x => x.AcceptedTypes!).NotEmpty()
            .When(x => x.AcceptedTypes is not null);
    }
}
