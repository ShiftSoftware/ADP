using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Decides how the trailing variant token of a milestone package code is treated — the part after
/// the milestone itself, which a deployment may use for trim, fuel type or service class.
/// <para>
/// Required by every milestone-based condition and deliberately not defaulted. Whether a qualified
/// code counts as the same service is a business question with no safe guess: reading it one way
/// silently withholds a reward the customer earned, and the other way grants one nobody intended.
/// Making it explicit turns that into an authoring decision rather than a discovery months later.
/// </para>
/// </summary>
[Docable]
public class EligibilityConditionQualifier
{
    /// <summary>How the trailing qualifier decides whether a milestone code takes part.</summary>
    public EligibilityConditionQualifierSelection Selection { get; set; }

    /// <summary>
    /// The qualifiers named by the selection. Required by
    /// <see cref="EligibilityConditionQualifierSelection.Only"/> and
    /// <see cref="EligibilityConditionQualifierSelection.Except"/>, and must be omitted by every
    /// other selection, which fails closed rather than quietly disregarding a list the author wrote.
    /// </summary>
    public IEnumerable<string> Values { get; set; }
}

/// <summary>
/// The qualifier strategies a milestone condition can take. Mirrors
/// <see cref="EligibilityConditionSelection"/>: some members require
/// <see cref="EligibilityConditionQualifier.Values"/> and the rest must omit it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EligibilityConditionQualifierSelection
{
    /// <summary>
    /// Only unqualified codes count — the milestone must be the last token. This is the literal
    /// reading of "the code ending with the milestone", and the most restrictive member.
    /// </summary>
    None = 0,

    /// <summary>Qualified or not, every milestone code counts.</summary>
    Any = 1,

    /// <summary>
    /// Allow-list: the qualifier must be one of <see cref="EligibilityConditionQualifier.Values"/>.
    /// An unqualified code carries no qualifier and so is never allowed by this member — use
    /// <see cref="Except"/> to accept unqualified codes alongside some qualified ones.
    /// </summary>
    Only = 2,

    /// <summary>
    /// Deny-list: the qualifier must not be one of <see cref="EligibilityConditionQualifier.Values"/>.
    /// An unqualified code carries nothing to deny, so it always counts.
    /// </summary>
    Except = 3,
}
