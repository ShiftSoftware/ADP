using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One service on the vehicle's menu — the DMS menu code and labour code, and the priced parts and labour
/// that compose them.
///
/// <para><b>Flat by design.</b> The service-menu lookup's own shape nests lines under variants
/// (<c>ServiceMenuLookupDTO</c> → <c>ServiceMenuVariantDTO</c> → <c>ServiceMenuLineDTO</c>) because a caller
/// asking for a model's menu is choosing a variant. A caller that started from a VIN is not: it wants a list
/// of services it can render, so the variant travels ON the line as
/// <see cref="VariantID"/>/<see cref="VariantName"/> and a UI groups client-side if it wants to. The lines
/// arrive in the nested shape's order — per variant, scheduled services by distance, then standalone.</para>
///
/// <para><b>The codes are the DMS export's codes.</b> They come off the same generator the export calls, over
/// data replicated per row into Cosmos. See ADP.Menus/COSMOS_REPLICATION_PLAN.md §1.1.</para>
///
/// <para><b>No cost, margin or profit field belongs here, ever.</b> Those are dealer figures; they live on the
/// DMS export's own line type (<c>MenuLineMargins</c>). The lookup never even asks the generator for cost, so
/// there is nothing to strip — but this is exactly the type one would get copied onto, so a test asserts no
/// property here names Cost, Profit or Margin.</para>
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceMenuLineDTO
{
    /// <summary>The menu variant this service belongs to, in the menus catalog.</summary>
    public long VariantID { get; set; }

    /// <summary>
    /// The variant's name as authored (a trim or drivetrain qualifier). A model usually has one variant; when
    /// it has several, this is what a UI groups or filters by.
    /// </summary>
    public string VariantName { get; set; }

    /// <summary>
    /// The variant's menu is offered free of charge. A variant-level flag travelling on the line, like
    /// <see cref="VariantName"/> — every line of a free variant carries it.
    ///
    /// <para><b>The money fields below are unaffected.</b> Nothing zeroes a total for a free variant, so a
    /// UI that renders <see cref="TotalPrice"/> verbatim quotes a customer for a menu the catalog calls
    /// free. Read this first and render "free" instead.</para>
    ///
    /// <para>Filter by it with <c>VehicleServiceMenuRequestOptions.FreeFilter</c>.</para>
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// Stable identity of this line, independent of language — use it to correlate the same service across
    /// language requests. Never key on <see cref="Code"/> for that: it is language-dependent by construction.
    /// </summary>
    public string LineKey { get; set; }

    /// <summary>The generated menu code, as the DMS knows it.</summary>
    public string Code { get; set; }

    /// <summary>The generated labour operation code.</summary>
    public string LabourCode { get; set; }

    /// <summary>
    /// The service interval's description for a scheduled service; the item's or group's name for a standalone
    /// one.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// What produced this line. Serialized as a string so the generated TypeScript union is honest about the
    /// wire format.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ServiceMenuLineType LineType { get; set; }

    /// <summary>Convenience over <see cref="LineType"/>; both standalone shapes report true.</summary>
    public bool IsStandalone { get; set; }

    /// <summary>The service interval's code. Null on standalone services.</summary>
    public string ServiceIntervalCode { get; set; }

    /// <summary>
    /// The odometer reading this service is due at, and the axis a schedule is read along. Null on standalone
    /// services. DESPITE THE NAME THIS IS IN KILOMETRES — render it as it is; dividing by 1000 quotes a
    /// 20,000 km service as 20 km. It carries <c>ServiceInterval.ValueInMeter</c> verbatim, and the catalogue
    /// authors <c>20000</c> there for the service it also names "20,000 KM".
    /// </summary>
    public int? ServiceIntervalValueInMeter { get; set; }

    /// <summary>The labour rate charged, per hour — the country rate, or the variant's primary rate.</summary>
    public decimal LabourRate { get; set; }

    /// <summary>Allowed time in hours.</summary>
    public decimal AllowedTime { get; set; }

    /// <summary><see cref="LabourRate"/> × <see cref="AllowedTime"/>.</summary>
    public decimal LabourPrice { get; set; }

    /// <summary>The consumable charge, already scaled by the transfer rate. Always 0 on standalone services.</summary>
    public decimal Consumable { get; set; }

    /// <summary><see cref="LabourPrice"/> + <see cref="Consumable"/>.</summary>
    public decimal LabourTotalPrice { get; set; }

    /// <summary>The parts this service uses, in the order the generator emitted them.</summary>
    public List<VehicleServiceMenuPartDTO> Parts { get; set; } = new List<VehicleServiceMenuPartDTO>();

    /// <summary>Sum of every part's <see cref="VehicleServiceMenuPartDTO.TotalPrice"/>.</summary>
    public decimal PartsTotalPrice { get; set; }

    /// <summary>The variant's discount percentage applied to this service. Null when there is none.</summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>The amount <see cref="DiscountPercentage"/> takes off. 0 when there is no discount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// What the customer pays: <see cref="LabourTotalPrice"/> + <see cref="PartsTotalPrice"/>, less the
    /// discount. The same arithmetic the DMS export's menu total uses.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// True when at least one part had no price row for the country, so its price — and therefore
    /// <see cref="PartsTotalPrice"/> and <see cref="TotalPrice"/> — is understated rather than genuinely zero.
    /// Mark the total as incomplete instead of quoting it.
    /// </summary>
    public bool HasUnpricedParts { get; set; }
}
