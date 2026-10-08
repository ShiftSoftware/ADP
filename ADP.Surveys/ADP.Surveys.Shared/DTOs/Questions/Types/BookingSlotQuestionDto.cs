using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

/// <summary>
/// A preferred appointment slot, picked from a branch calendar's availability. The renderer
/// fetches the available days and times from <see cref="CalendarApi"/> and stores the chosen
/// slot as the branch's local wall-clock time, <c>yyyy-MM-ddTHH:mm</c>, with no offset.
/// <see cref="BranchId"/>, <see cref="DepartmentId"/>, <see cref="Services"/> and <see cref="BrandId"/>
/// are each a fixed value or a token such as <c>{{answers.branch}}</c>, resolved when the question renders.
/// With no department, the renderer books the first department of <see cref="Services"/>, or all of
/// them with <see cref="MergeDepartments"/>.
/// Availability is not re-checked on submit: the answer is a preference, not a reservation.
/// </summary>
public class BookingSlotQuestionDto : QuestionDto
{
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.BookingSlot;

    [JsonPropertyName("calendarApi")]
    public string CalendarApi { get; set; } = "";

    [JsonPropertyName("branchId")]
    public string BranchId { get; set; } = "";

    [JsonPropertyName("departmentId")]
    public string DepartmentId { get; set; } = "";

    [JsonPropertyName("services")]
    public string Services { get; set; } = "";

    [JsonPropertyName("mergeDepartments")]
    public bool MergeDepartments { get; set; }

    [JsonPropertyName("brandId")]
    public string BrandId { get; set; } = "";

    public const string SlotFormat = "yyyy-MM-dd'T'HH:mm";
}

public class BookingSlotQuestionDtoValidator : QuestionDtoBaseValidator<BookingSlotQuestionDto>
{
    public BookingSlotQuestionDtoValidator()
    {
        RuleFor(x => x.CalendarApi).NotEmpty()
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            .WithMessage("calendarApi must be an absolute http(s) URL.");
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty().When(x => string.IsNullOrWhiteSpace(x.Services))
            .WithMessage("departmentId or services is required.");
        RuleFor(x => x.BrandId).NotEmpty();
    }
}
