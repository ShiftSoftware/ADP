using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;

/// <summary>
/// Admin/CRUD DTO for a banked question. Deliberately not <c>BankQuestionDto</c> (lowercase)
/// — that's the schema DTO that ships inside resolved survey JSON. This one carries the
/// ShiftEntity envelope (audit fields, HashId-encoded ID) plus <see cref="BankEntryID"/>
/// — the stable Decision #11 anchor that's fixed at insert time.
/// </summary>
public class BankQuestionAdminDTO : ShiftEntityViewAndUpsertDTO
{
    [BankQuestionHashId]
    public override string? ID { get; set; }

    /// <summary>
    /// Server-assigned on create, immutable after that. The <c>SurveyAnswer.BankEntryID</c>
    /// FK points here — this is the anchor typo corrections to <see cref="Key"/> survive.
    /// </summary>
    public Guid BankEntryID { get; set; }

    /// <summary>Human-readable key. Editable (incl. when <see cref="Locked"/>) for typo correction.</summary>
    public string Key { get; set; } = default!;

    /// <summary>The full canonical question definition — type, validation, options, etc.
    /// Nullable so that the JSON-editor MVP can bind an empty textarea and surface proper
    /// inline validation errors when the user has typed something invalid; hard non-null
    /// enforcement happens server-side in <c>BankQuestionRepository.UpsertAsync</c>.</summary>
    public QuestionDto? Question { get; set; }

    public string? BiColumn { get; set; }

    /// <summary>Read-only from client — server flips to <c>true</c> on first publish reference.</summary>
    public bool Locked { get; set; }

    public bool Retired { get; set; }

    public List<string>? Tags { get; set; }
}

public class BankQuestionAdminDTOValidator : AbstractValidator<BankQuestionAdminDTO>
{
    public BankQuestionAdminDTOValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        // Inner Question shape / required-ness is enforced server-side in the repo
        // layer — a null Question surfaces a structured error through the snackbar.
        // Matches the SurveyAdminDTO "When(Draft is not null, ...)" pattern.
    }
}
