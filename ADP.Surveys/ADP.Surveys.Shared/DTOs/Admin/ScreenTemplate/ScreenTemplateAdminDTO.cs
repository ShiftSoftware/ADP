using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;

public class ScreenTemplateAdminDTO : ShiftEntityViewAndUpsertDTO
{
    [ScreenTemplateHashId]
    public override string? ID { get; set; }

    /// <summary>Human-readable template id. Surveys reference it via <c>templateRef</c>.</summary>
    public string Key { get; set; } = default!;

    /// <summary>The full schema-level template (title, description, banked-question refs).
    /// Nullable for the same reason as <c>BankQuestionAdminDTO.Question</c> — lets the
    /// JSON-editor MVP bind empty state cleanly; repo layer enforces non-null server-side.</summary>
    public ScreenTemplateDto? Template { get; set; }

    public List<string>? Tags { get; set; }
}

public class ScreenTemplateAdminDTOValidator : AbstractValidator<ScreenTemplateAdminDTO>
{
    public ScreenTemplateAdminDTOValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        // When populated, the template's own shape validator still runs so inline
        // errors surface as the user types. Empty Template is caught server-side.
        When(x => x.Template is not null, () =>
            RuleFor(x => x.Template!).SetValidator(new ScreenTemplateDtoValidator()));
    }
}
