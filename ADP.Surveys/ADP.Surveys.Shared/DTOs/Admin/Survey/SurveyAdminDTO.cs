using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;

/// <summary>
/// Survey admin/CRUD DTO. <see cref="Draft"/> carries the full schema <c>SurveyDto</c>
/// which the builder edits as one unit (JSON-blob authoring — see hybrid-normalization
/// note in Phase 1 Part C). The schema DTO's own FluentValidation runs via
/// <see cref="SurveyAdminDTOValidator"/> below.
/// </summary>
public class SurveyAdminDTO : ShiftEntityViewAndUpsertDTO
{
    [SurveyHashId]
    public override string? ID { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>The editable schema draft. Null on create — server stamps an empty-but-valid skeleton.</summary>
    public SurveyDto? Draft { get; set; }

    /// <summary>Read-only. Surfaces the current published version number for the builder's header.</summary>
    public int? PublishedVersionNumber { get; set; }
}

public class SurveyAdminDTOValidator : AbstractValidator<SurveyAdminDTO>
{
    public SurveyAdminDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        When(x => x.Draft is not null, () =>
            RuleFor(x => x.Draft!).SetValidator(new SurveyDtoValidator()));
    }
}
