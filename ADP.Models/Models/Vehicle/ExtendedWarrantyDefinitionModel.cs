using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Defines an extended-warranty coverage that can be awarded from declarative vehicle lookup
/// eligibility conditions. The condition contract is shared with service-item eligibility so
/// hosts can use the same external JSON grammar and matching semantics. Qualifying coverage
/// begins at the end of the vehicle's standard warranty and lasts for the configured duration.
/// </summary>
[Docable]
public class ExtendedWarrantyDefinitionModel
{
    /// <summary>A stable identifier for this extended-warranty definition.</summary>
    public string ID { get; set; }

    /// <summary>
    /// The name shown for this coverage. The identifier is not a display string, so when this is
    /// omitted consumers fall back to their own generic "extended warranty" wording.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The provider company's Identity ID. When omitted, the lookup host's configured
    /// <c>LookupOptions.DistributorCompanyID</c> is used.
    /// </summary>
    public long? ProviderCompanyID { get; set; }

    /// <summary>
    /// The brand IDs this coverage is awarded for. Omit it to award every brand, which is what
    /// every definition did before this property existed; an empty list awards none.
    /// <para>
    /// Brand is a fact about the vehicle rather than a predicate over its history, so it is stated
    /// here rather than through <see cref="EligibilityConditions"/> — the same separation service
    /// items draw between their own <c>BrandIDs</c> and their conditions. A programme that runs for
    /// one brand and not another is two definitions, each carrying its own conditions, rather than
    /// one definition whose conditions have to encode which brand they are talking about.
    /// </para>
    /// </summary>
    public IEnumerable<long?> BrandIDs { get; set; }

    /// <summary>The number of duration units for which the coverage remains valid.</summary>
    public int? ActiveFor { get; set; }

    /// <summary>The duration unit used with <see cref="ActiveFor"/>.</summary>
    public DurationType? ActiveForDurationType { get; set; }

    /// <summary>
    /// Declarative predicates that must all match before this coverage is awarded. This is the
    /// same closed condition grammar service items are gated by; unsupported conditions fail
    /// closed.
    /// </summary>
    public IEnumerable<EligibilityConditionModel> EligibilityConditions { get; set; }
}
