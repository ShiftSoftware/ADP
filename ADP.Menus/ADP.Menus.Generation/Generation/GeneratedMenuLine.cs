using System.Collections.Generic;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>What produced a line — the three shapes the generator emits.</summary>
public enum MenuLineType
{
    /// <summary>A service interval the variant is periodically available for.</summary>
    Periodic = 0,

    /// <summary>A standalone item that belongs to no standalone group — one line per item.</summary>
    StandaloneUngrouped = 1,

    /// <summary>A standalone group — one line folded from every item in the group.</summary>
    StandaloneGrouped = 2,
}

/// <summary>
/// One generated menu line — the shared result every consumer maps out of.
///
/// DESIGN: this type carries the generated codes AND every input that composed them. Consumers live
/// outside this package (the report exporter is in the host application, the vehicle lookup is in
/// ADP.LookupServices), so they must be able to render, re-compose or re-format a line WITHOUT
/// re-reading the menus database or being handed the mapping dictionaries on the side. Anything the
/// generator resolved on the way to a code is therefore surfaced here rather than discarded.
///
/// Deliberately NOT carried: the DMS report's derived margin/profit arithmetic. Those are pure
/// functions of the fields below, so the report layer computes them itself instead of the generator
/// taking on presentation concerns.
///
/// Components are nullable because not every line has every one (a standalone line has no service
/// interval; an unmapped brand has no abbreviation or company code).
/// </summary>
public class GeneratedMenuLine
{
    // ---- identity -----------------------------------------------------------------------------

    /// <summary>
    /// Stable identity of this line within its request, independent of language:
    /// <c>"P|{variantId}|{serviceIntervalId}"</c> for periodic,
    /// <c>"S|{variantId}|{menuItemId}"</c> for ungrouped standalone,
    /// <c>"G|{variantId}|{standaloneGroupId}"</c> for grouped standalone.
    ///
    /// Use it to correlate the same line across languages (the code and description vary by language;
    /// this does not). Never key on <see cref="Code"/> for that — it is language-dependent.
    /// </summary>
    public string LineKey { get; set; }

    public MenuLineType LineType { get; set; }

    /// <summary>Convenience over <see cref="LineType"/>; both standalone shapes report true.</summary>
    public bool IsStandalone => LineType != MenuLineType.Periodic;

    // ---- the generated outputs ----------------------------------------------------------------

    /// <summary>The generated menu code, for <see cref="Language"/>.</summary>
    public string Code { get; set; }

    /// <summary>The generated labour operation code.</summary>
    public string LabourCode { get; set; }

    /// <summary>
    /// Periodic lines: the service interval's description, verbatim (NOT language-resolved).
    /// Standalone lines: the replacement item's friendly name, or the standalone group's name.
    /// </summary>
    public string Description { get; set; }

    // ---- what composed Code -------------------------------------------------------------------
    // Periodic:            "{MenuCodePrefix} {BasicModelCode} {MenuCodeSegment} {MenuCodePostfix}"
    // Standalone (either): "{MenuCodePrefix} {MenuCodeSegment} {BasicModelCode} {MenuCodePostfix}"
    // ...each trimmed. Note the segment and the model code swap places between the two shapes, so
    // reconstruction needs LineType.

    /// <summary>Language-resolved menu prefix (the variant's periodic or standalone prefix).</summary>
    public string MenuCodePrefix { get; set; }

    /// <summary>
    /// Language-resolved middle segment: the service interval code (periodic), the replacement item's
    /// standalone operation code (ungrouped), or the standalone group's menu code (grouped).
    /// </summary>
    public string MenuCodeSegment { get; set; }

    /// <summary>Language-resolved menu postfix (the variant's periodic or standalone postfix).</summary>
    public string MenuCodePostfix { get; set; }

    // ---- what composed LabourCode -------------------------------------------------------------
    // "{LabourOperationCode}{AllowedTimeText}{LabourRateCode}{BrandAbbreviation}", trimmed.

    /// <summary>
    /// Leading segment: the service-interval group's labour code (periodic), the replacement item's
    /// standalone labour code (ungrouped), or the standalone group's labour code (grouped).
    /// </summary>
    public string LabourOperationCode { get; set; }

    /// <summary>
    /// <see cref="AllowedTime"/> rendered for embedding in the labour code. Not a plain number —
    /// see <see cref="MenuTextHelpers.GetAllowedTimeText"/> for the padding and trimming rules.
    /// </summary>
    public string AllowedTimeText { get; set; }

    /// <summary>
    /// The labour-rate mapping's code, resolved from (brand, <see cref="PrimaryLabourRate"/>).
    /// Null only if a future generator tolerates a missing mapping; today it always resolves or throws.
    /// </summary>
    public string LabourRateCode { get; set; }

    /// <summary>
    /// The brand mapping's abbreviation, or "Z" when the brand has no mapping row. Never null today,
    /// but nullable so a consumer can distinguish "no mapping" if the fallback is ever removed.
    /// </summary>
    public string BrandAbbreviation { get; set; }

    // ---- brand / model ------------------------------------------------------------------------

    public string BasicModelCode { get; set; }

    public long? BrandID { get; set; }

    /// <summary>
    /// The brand mapping's code — the "company code" a DMS export writes alongside the menu code.
    /// Distinct from <see cref="BrandAbbreviation"/> (which feeds the labour code). Null when the
    /// brand has no mapping row; there is no fallback for this one.
    /// </summary>
    public string BrandCode { get; set; }

    /// <summary>The vehicle model name.</summary>
    public string Model { get; set; }

    public long VariantID { get; set; }
    public string VariantName { get; set; }

    /// <summary>
    /// The variant's "menu is free of charge" flag, stamped onto every line it produced. It takes part in
    /// no arithmetic — the money fields below are the same whether it is set or not — so a consumer that
    /// quotes a free menu as 0 does that itself.
    /// </summary>
    public bool IsFree { get; set; }

    // ---- source-row correlation ---------------------------------------------------------------

    /// <summary>Periodic lines only.</summary>
    public long? ServiceIntervalID { get; set; }

    /// <summary>Periodic lines only — the interval group whose labour details supplied the time.</summary>
    public long? ServiceIntervalGroupID { get; set; }

    /// <summary>Ungrouped standalone lines only.</summary>
    public long? MenuItemID { get; set; }

    /// <summary>Grouped standalone lines only.</summary>
    public long? StandaloneGroupID { get; set; }

    /// <summary>Periodic lines only.</summary>
    public string ServiceIntervalCode { get; set; }

    /// <summary>Periodic lines only.</summary>
    public int? ServiceIntervalValueInMeter { get; set; }

    // ---- money --------------------------------------------------------------------------------

    /// <summary>
    /// The rate ON the line: the country labour rate, or the primary rate when no country rate matches
    /// or the config asked for the primary one.
    /// </summary>
    public decimal LabourRate { get; set; }

    /// <summary>
    /// The variant's primary rate — the labour-rate-mapping lookup key. Surfaced separately because it
    /// drives the labour CODE even when <see cref="LabourRate"/> differs.
    /// </summary>
    public decimal PrimaryLabourRate { get; set; }

    public decimal AllowedTime { get; set; }

    /// <summary>Scaled by <see cref="TransferRate"/> and rounded to 2 decimals. Always 0 on standalone lines.</summary>
    public decimal Consumable { get; set; }

    /// <summary>The consumable before transfer-rate scaling. Always 0 on standalone lines.</summary>
    public decimal RawConsumable { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public List<GeneratedMenuPart> Parts { get; set; } = [];

    // ---- the configuration this line was generated under ---------------------------------------
    // Echoed so a line is self-describing once results from several runs (languages, countries) are
    // merged, and so a consumer can un-scale or re-key without being handed the config separately.

    public long CountryID { get; set; }
    public decimal TransferRate { get; set; }
    public string Language { get; set; }
}

public class GeneratedMenuPart
{
    public string PartNumber { get; set; }

    /// <summary>The menu item this part came from — meaningful on grouped lines, which fold several items.</summary>
    public long MenuItemID { get; set; }

    /// <summary>The part's authored sort order within its item. The generator does not sort by it.</summary>
    public int SortOrder { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>
    /// Dealer cost — <c>null</c> unless <see cref="MenuGenerationConfig.IncludePartCost"/> was set.
    ///
    /// Dealer cost must never reach a public web component, so the generator omits it by DEFAULT and
    /// the DMS export opts in. That way a consumer cannot leak it by forgetting to strip it: the value
    /// is never in the object graph in the first place.
    ///
    /// When cost IS requested, a part with no matching country price row gets <c>0</c> (not null) —
    /// matching the export's fallback. So null means "not requested", never "not priced"; use
    /// <see cref="HasCountryPrice"/> for that.
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>Retail price.</summary>
    public decimal Price { get; set; }

    /// <summary>
    /// False when no country price row matched, in which case <see cref="Cost"/> and
    /// <see cref="Price"/> are 0 by fallback rather than because the part is genuinely free.
    /// </summary>
    public bool HasCountryPrice { get; set; }

    /// <summary>Null whenever <see cref="Cost"/> is — i.e. whenever cost was not requested.</summary>
    public decimal? TotalCost => Cost * Quantity;

    public decimal TotalPrice => Price * Quantity;
}
