using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// One canonical question in the Question Bank. Per Decision #11, the stable join
/// anchor for BI is <see cref="BankEntryID"/> (a GUID that never changes), while the
/// human-readable <see cref="Key"/> is what surveys reference via <c>bankRef</c> and
/// what shows up on answer submissions — and is typo-correctable after the fact.
///
/// Fixing a typo in <see cref="Key"/> retroactively updates BI column names for every
/// prior banked answer because responses join on <see cref="BankEntryID"/>, not on key.
/// Type / validation / answer shape (all inside <see cref="QuestionJson"/>) are frozen
/// per Decision #9 once <see cref="Locked"/> flips true.
/// </summary>
[TemporalShiftEntity]
public class BankQuestion : ShiftEntity<BankQuestion>
{
    /// <summary>Stable internal reference per Decision #11. Never changes after insert.</summary>
    public Guid BankEntryID { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable key. Surveys reference this via <c>bankRef</c>. Editable
    /// (for typo corrections) even when <see cref="Locked"/> is true.
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Full canonical <c>QuestionDto</c> serialized via <c>SurveySchemaSerializer.Options</c>.
    /// Includes type, title, validation rules, options, etc.
    /// </summary>
    public string QuestionJson { get; set; } = default!;

    /// <summary>Optional friendly column name for pivoted BI exports.</summary>
    public string? BiColumn { get; set; }

    /// <summary>True once any published survey references this entry. Enforced server-side on edits.</summary>
    public bool Locked { get; set; }

    /// <summary>Retired entries stay for historical exports but hide from the builder's "insert from bank" picker.</summary>
    public bool Retired { get; set; }

    /// <summary>Comma-separated tag list. Kept denormalized for simplicity; a separate table is overkill at current scope.</summary>
    public string? Tags { get; set; }

    public virtual ICollection<SurveyAnswer> Answers { get; set; } = new HashSet<SurveyAnswer>();
}
