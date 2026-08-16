using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// A declarative eligibility predicate evaluated against data exposed by the vehicle lookup.
/// <para>
/// The grammar started out gating service items and is now shared — extended-warranty definitions
/// use the same conditions, matched the same way — so it is named for what it does rather than for
/// the first thing that used it.
/// </para>
/// </summary>
[Docable]
public class EligibilityConditionModel
{
    /// <summary>
    /// The fully-qualified vehicle lookup field path to evaluate. Supported paths are defined
    /// by the lookup evaluator; this model does not imply arbitrary vehicle lookup traversal.
    /// </summary>
    public string Field { get; set; }

    /// <summary>How the configured values are combined across the scoped field values.</summary>
    public EligibilityConditionOperator Operator { get; set; }

    /// <summary>
    /// How each configured value is matched against a scoped field value. Defaults to exact
    /// matching so catalogs created before this property was added keep their existing behavior.
    /// </summary>
    public EligibilityConditionValueMatch ValueMatch { get; set; } =
        EligibilityConditionValueMatch.Exact;

    /// <summary>The values required by the comparison.</summary>
    public IEnumerable<string> Values { get; set; }

    /// <summary>Optional selection scope for collection-based fields.</summary>
    public EligibilityConditionScope Scope { get; set; }
}
