using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// One menu line: the DMS menu code and labour code, plus the priced parts and labour that compose it.
///
/// <b>No cost, margin or profit field belongs here.</b> Those are dealer figures and live on the DMS
/// export's own line type; the lookup never even asks the generator for cost. A test asserts this type
/// carries no such property, because this is precisely where one would get copied across by accident.
/// </summary>
[Docable]
public class ServiceMenuLineDTO
{
    /// <summary>
    /// Stable identity of this line, independent of language — use it to correlate the same line across
    /// language requests. Never key on <see cref="Code"/> for that: it is language-dependent.
    /// </summary>
    public string LineKey { get; set; }

    /// <summary>The generated menu code, as the DMS knows it.</summary>
    public string Code { get; set; }

    /// <summary>The generated labour operation code.</summary>
    public string LabourCode { get; set; }

    /// <summary>
    /// The service interval's description for a scheduled line; the item's or group's name for a
    /// standalone one.
    /// </summary>
    public string Description { get; set; }

    /// <summary>What produced this line.</summary>
    public ServiceMenuLineType LineType { get; set; }

    /// <summary>Convenience over <see cref="LineType"/>; both standalone shapes report true.</summary>
    public bool IsStandalone { get; set; }

    /// <summary>The service interval's code. Null on standalone lines.</summary>
    public string ServiceIntervalCode { get; set; }

    /// <summary>
    /// The interval's distance in metres, and the sort key for the schedule. Null on standalone lines.
    /// </summary>
    public int? ServiceIntervalValueInMeter { get; set; }

    /// <summary>The labour rate charged, per hour — the country rate, or the variant's primary rate.</summary>
    public decimal LabourRate { get; set; }

    /// <summary>Allowed time in hours.</summary>
    public decimal AllowedTime { get; set; }

    /// <summary><see cref="LabourRate"/> × <see cref="AllowedTime"/>.</summary>
    public decimal LabourPrice { get; set; }

    /// <summary>
    /// The consumable charge, already scaled by the transfer rate. Always 0 on standalone lines.
    /// </summary>
    public decimal Consumable { get; set; }

    /// <summary><see cref="LabourPrice"/> + <see cref="Consumable"/>.</summary>
    public decimal LabourTotalPrice { get; set; }

    /// <summary>The parts on this line, in the order the generator emitted them.</summary>
    public List<ServiceMenuPartDTO> Parts { get; set; } = new List<ServiceMenuPartDTO>();

    /// <summary>Sum of every part's <see cref="ServiceMenuPartDTO.TotalPrice"/>.</summary>
    public decimal PartsTotalPrice { get; set; }

    /// <summary>The variant's discount percentage applied to this line. Null when there is none.</summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>The amount <see cref="DiscountPercentage"/> takes off. 0 when there is no discount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// What the customer pays: <see cref="LabourTotalPrice"/> + <see cref="PartsTotalPrice"/>, less the
    /// discount. The same arithmetic the DMS export's menu total uses.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// True when at least one part on the line had no price row for the requested country, so its price
    /// — and therefore <see cref="PartsTotalPrice"/> and <see cref="TotalPrice"/> — is understated rather
    /// than genuinely zero. Lets a UI mark the total as incomplete instead of quoting it.
    /// </summary>
    public bool HasUnpricedParts { get; set; }
}
