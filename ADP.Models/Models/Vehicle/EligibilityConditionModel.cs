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
    private IEnumerable<EligibilityConditionModel> anyOf;

    /// <summary>
    /// One non-empty OR group of ordinary leaf conditions. Nested groups are unsupported.
    /// The group owns WhenUnmet; alternatives must retain the default Hide behavior.
    /// Omit field, values, scope, program and qualifier on the group itself.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
    public IEnumerable<EligibilityConditionModel> AnyOf
    {
        get => anyOf;
        set => anyOf = value ?? System.Array.Empty<EligibilityConditionModel>();
    }

    // Json.NET must read explicit null through the setter (invalid empty group), but omit
    // absent groups on ordinary leaves when writing. NullValueHandling.Ignore also skips reads.
    public bool ShouldSerializeAnyOf() => anyOf is not null;

    /// <summary>Retains unsupported JSON members so persisted conditions cannot silently lose a rule.</summary>
    [DocIgnore]
    [System.Text.Json.Serialization.JsonExtensionData]
    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> UnsupportedProperties { get; set; }

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

    /// <summary>
    /// The programmes whose codes take part in a milestone comparison, matched against the leading
    /// token of the code. Omit it to accept every programme.
    /// <para>
    /// A list rather than a single value from the outset: a deployment that later awards a second
    /// programme under the same rule should not need a breaking change to say so.
    /// </para>
    /// <para>
    /// Only milestone-based conditions read this. On any other condition it is an authoring mistake
    /// and fails closed, because the author plainly meant something the comparison would ignore.
    /// </para>
    /// </summary>
    public IEnumerable<string> Program { get; set; }

    /// <summary>
    /// How the trailing variant token of a milestone code is treated. Required by every
    /// milestone-based condition, and an authoring mistake on any other, which fails closed.
    /// </summary>
    public EligibilityConditionQualifier Qualifier { get; set; }

    /// <summary>The values required by the comparison.</summary>
    public IEnumerable<string> Values { get; set; }

    /// <summary>Optional selection scope for collection-based fields.</summary>
    public EligibilityConditionScope Scope { get; set; }

    /// <summary>
    /// What failing this condition means for the item — hidden, locked, or missed. Defaults to
    /// hiding, which is what every condition did before this property existed.
    /// <para>
    /// An item is hidden if any hiding condition fails, locked if any locking one does, and missed
    /// otherwise: a customer who has not finished their prerequisites has not missed anything, even
    /// though both clauses are failing at once.
    /// </para>
    /// </summary>
    public EligibilityConditionUnmetBehavior WhenUnmet { get; set; } =
        EligibilityConditionUnmetBehavior.Hide;
}
