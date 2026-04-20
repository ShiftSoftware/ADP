using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// One submitted answer, keyed by question. Per Decision #11:
/// <list type="bullet">
///   <item>Banked questions: <see cref="BankEntryID"/> holds the <see cref="BankQuestion.BankEntryID"/>
///     GUID (the stable BI join anchor). <see cref="KeyAtSubmission"/> holds the human-readable
///     key as it was at the moment of submission, for audit / forensics.</item>
///   <item>Inline questions: <see cref="BankEntryID"/> is null; <see cref="KeyAtSubmission"/> holds
///     the survey-local question id.</item>
/// </list>
///
/// Exports project the <em>current</em> <see cref="BankQuestion.Key"/> for banked answers
/// (so typo corrections retroactively fix BI column names), falling back to
/// <see cref="KeyAtSubmission"/> for inline answers.
/// </summary>
public class SurveyAnswer : ShiftEntity<SurveyAnswer>
{
    public long SurveyResponseID { get; set; }
    public SurveyResponse SurveyResponse { get; set; } = default!;

    /// <summary>Non-null when the answer is for a banked question — the stable join anchor.</summary>
    public Guid? BankEntryID { get; set; }
    public BankQuestion? BankQuestion { get; set; }

    /// <summary>Human-readable key as submitted. Banked or survey-local.</summary>
    public string KeyAtSubmission { get; set; } = default!;

    /// <summary>
    /// Raw answer value as a JSON string. Scalar types (text, number, bool, enum ids)
    /// serialize as the scalar; multi-value types (multiChoice) serialize as a JSON array.
    /// </summary>
    public string ValueJson { get; set; } = default!;

    /// <summary>Ordering hint for multi-value types where order matters; 0 for scalars.</summary>
    public int Order { get; set; }
}
