using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Service.Cosmos;

// LAYER 2 of ADP.Menus/COSMOS_REPLICATION_PLAN.md — persistence only. These are what replication
// WRITES and the vehicle lookup READS; they are never the generation service's input. Kept together in
// one file because they describe one storage design and only make sense as a set.
//
// The design is §16's, which follows the ShiftIdentity CompanyBranches pattern:
//
//   Services database
//   ├── ServiceIntervals / ServiceIntervalGroups / ReplacementItems /
//   │   StandaloneReplacementItemGroups / LabourRateMappings / BrandMappings
//   │        ← every MASTER entity gets its own container, partitioned by its own id
//   └── ServiceMenus              partition: /BasicModelCode then /ItemType
//         ItemType="MenuVariant"  → the root; embeds the menu and vehicle model, owns country rates
//         ItemType="MenuPeriod"   ┐
//         ItemType="MenuLabour"   ├─ sibling link documents in the SAME model-code partition, each
//         ItemType="MenuItem"     ┘  carrying the related master entity's fields (MenuItem also owns
//                                    its parts and their country prices)
//
// Three conventions run through all of them:
//
//  • A container's partition key is something every document in it genuinely has. The master entities
//    have no basic model code, so they are NOT forced into the menu container — they get their own.
//
//  • The ServiceMenus documents are FULLY DENORMALIZED: interval codes and descriptions, group labour
//    codes and interval membership, replacement-item operation codes and friendly names, labour-rate
//    codes and brand abbreviations are all embedded. A lookup is therefore ONE partition query by
//    basic model code and nothing else — no reference cache, no second round trip, no staleness
//    window in the reader. Keeping the copies fresh is replication's job (UpdateReference fan-outs),
//    not the reader's.
//
//  • Every document carries IsDeleted. Soft deleting a row is an ordinary UPDATE, so the replication
//    trigger UPSERTS the document rather than removing it — only a hard delete removes one. Readers
//    must therefore filter on this flag; it is not optional bookkeeping. The same rule extends to the
//    embedded copies: AN EMBEDDED REFERENCE THAT IS NULL OR IsDeleted CONTRIBUTES NOTHING. Both write
//    paths honour that — the initial projection embeds only live master rows, and a fan-out that
//    carries a soft delete through embeds the row with IsDeleted set.

// ---- master containers ----------------------------------------------------------------------------
// One container each, partitioned by /id. These are also the shapes embedded into the ServiceMenus
// documents below, so a master row has exactly one projection and it cannot drift between the two
// places it lands.

/// <summary>
/// A service interval. Its <see cref="Code"/> is a component of every periodic menu code, and its
/// <see cref="Description"/> is the periodic line's description.
/// </summary>
[Docable]
public class ServiceIntervalCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long ServiceIntervalID { get; set; }

    public string Code { get; set; }

    /// <summary>Used verbatim as a periodic line's description — never language-resolved.</summary>
    public string Description { get; set; }

    public int ValueInMeter { get; set; }

    public long ServiceIntervalGroupID { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>
/// A service-interval group, carrying its interval membership so generation can decide which labour
/// detail (and which item's parts) belong to a given periodic line without a second lookup.
/// </summary>
[Docable]
public class ServiceIntervalGroupCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long ServiceIntervalGroupID { get; set; }

    /// <summary>Leading component of a periodic line's labour code.</summary>
    public string LabourCode { get; set; }

    /// <summary>Live members only — a soft-deleted interval is not a member.</summary>
    public List<long> ServiceIntervalIDs { get; set; } = [];

    public bool IsDeleted { get; set; }
}

/// <summary>
/// A replacement item — the thing a menu item applies to a variant. Supplies the standalone menu and
/// labour code segments, the ungrouped line's description, and the interval groups whose periodic
/// lines its parts join.
/// </summary>
[Docable]
public class ReplacementItemCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long ReplacementItemID { get; set; }

    /// <summary>Description of an ungrouped standalone line.</summary>
    public string FriendlyName { get; set; }

    /// <summary>Raw, possibly a multi-language JSON object — resolved per language at generation time.</summary>
    public string StandaloneOperationCode { get; set; }

    public string StandaloneLabourCode { get; set; }

    /// <summary>Null when the item is ungrouped.</summary>
    public long? StandaloneReplacementItemGroupID { get; set; }

    /// <summary>
    /// The interval groups this replacement item serves, live links only. Flat so it is queryable with
    /// ARRAY_CONTAINS — it is what a service-interval-group fan-out finds its documents by. The groups'
    /// own denormalized detail lives on <see cref="MenuItemCosmosModel.ServiceIntervalGroups"/>.
    /// </summary>
    public List<long> ServiceIntervalGroupIDs { get; set; } = [];

    public bool IsDeleted { get; set; }
}

/// <summary>
/// A standalone replacement-item group. Items belonging to one fold into a single standalone line, and
/// this supplies that line's code segments and description.
/// </summary>
[Docable]
public class StandaloneReplacementItemGroupCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long StandaloneReplacementItemGroupID { get; set; }

    /// <summary>Description of a grouped standalone line.</summary>
    public string Name { get; set; }

    /// <summary>Raw, possibly a multi-language JSON object.</summary>
    public string MenuCode { get; set; }

    public string LabourCode { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>(brand, primary labour rate) → code. Feeds the labour code.</summary>
[Docable]
public class LabourRateMappingCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long? BrandID { get; set; }

    public decimal LabourRate { get; set; }

    public string Code { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>
/// A brand's mapping. Two distinct codes: <see cref="BrandAbbreviation"/> is concatenated into the
/// labour code, while <see cref="Code"/> is the company code a DMS export writes as its own column.
/// </summary>
[Docable]
public class BrandMappingCosmosModel
{
    /// <summary>The row id — also the container's partition key.</summary>
    [DocIgnore] public string id { get; set; }

    public long? BrandID { get; set; }

    public string Code { get; set; }

    public string BrandAbbreviation { get; set; }

    public bool IsDeleted { get; set; }
}

// ---- the ServiceMenus container -------------------------------------------------------------------
// Partition key: /BasicModelCode then /ItemType. Every document here genuinely has a basic model code.

/// <summary>
/// One menu variant — the root document of a model's menu graph. The parent menu and its vehicle model
/// are flattened onto it, and the labour-rate and brand mappings its generated codes depend on are
/// embedded, so nothing outside this partition is needed to generate the variant's lines.
/// </summary>
[Docable]
public class MenuVariantCosmosModel : IPartitionedItem
{
    /// <summary>The variant's row id.</summary>
    [DocIgnore] public string id { get; set; }

    /// <summary>Partition key level 1 — from the parent menu.</summary>
    public string BasicModelCode { get; set; }

    /// <summary>Partition key level 2.</summary>
    [DocIgnore] public string ItemType => ModelTypes.MenuVariant;

    public long VariantID { get; set; }

    /// <summary>From the parent menu's vehicle model. Selects the labour-rate and brand mappings.</summary>
    public long? BrandID { get; set; }

    /// <summary>The parent menu's vehicle model name.</summary>
    public string Model { get; set; }

    public string VariantName { get; set; }

    /// <summary>Raw, possibly a multi-language JSON object — resolved per language at generation time.</summary>
    public string MenuPrefix { get; set; }
    public string MenuPostfix { get; set; }
    public string StandaloneMenuPrefix { get; set; }
    public string StandaloneMenuPostfix { get; set; }

    /// <summary>The variant's primary labour rate — the labour-rate-mapping lookup key.</summary>
    public decimal LabourRate { get; set; }

    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// The variant's menu is offered free of charge. Carried, never computed on: it changes no generated
    /// price, so a reader that filters or renders on it owns that decision.
    ///
    /// <para>Defaults to <c>false</c>, so documents written before this field existed read as not-free
    /// until a catch-up sweep refreshes them.</para>
    /// </summary>
    public bool IsFree { get; set; }

    public bool HasStandaloneItems { get; set; }

    /// <summary>
    /// The PARENT MENU's soft-delete flag, flattened onto the variant.
    ///
    /// The DMS export selects variants with <c>!variant.IsDeleted &amp;&amp; !variant.Menu.IsDeleted</c>, so a
    /// reader needs both to reproduce its line set. Deleting a menu does not cascade to its variants
    /// (COSMOS_REPLICATION_PLAN.md §17, "deletes do not cascade"), so without this the variant document
    /// stays <c>IsDeleted = false</c> and the lookup keeps serving menu codes for a deleted menu — with
    /// no error anywhere. Carried here rather than resolved at read time because the reader sees one
    /// partition and never the Menu row.
    ///
    /// Defaults to <c>false</c>, so documents written before this field existed keep their previous
    /// behaviour until a catch-up sweep refreshes them.
    /// </summary>
    public bool MenuIsDeleted { get; set; }

    /// <summary>
    /// Per-country labour rates, embedded — they are owned by the variant and never queried alone.
    /// Carried unfiltered, soft-delete flag and all, so the generator keeps owning the inclusion rule.
    /// </summary>
    public List<MenuCountryLabourRateCosmosModel> CountryLabourRates { get; set; } = [];

    /// <summary>
    /// The mapping for (<see cref="BrandID"/>, <see cref="LabourRate"/>), embedded. Null when the pair
    /// has no mapping row — which is exactly the case the generator throws on, so a reader must leave
    /// the entry out of its dictionary rather than inventing a code.
    /// </summary>
    public LabourRateMappingCosmosModel LabourRateMapping { get; set; }

    /// <summary>
    /// The mapping for <see cref="BrandID"/>, embedded. Null when the brand is unmapped, which is
    /// valid — the generator falls back to the "Z" abbreviation and a null company code.
    /// </summary>
    public BrandMappingCosmosModel BrandMapping { get; set; }

    public bool IsDeleted { get; set; }
}

public class MenuCountryLabourRateCosmosModel
{
    public long CountryID { get; set; }
    public decimal LabourRate { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// A service interval the variant is periodically available for — the variant-to-interval link, with
/// the interval's own fields embedded so a periodic line's code and description need no second lookup.
/// </summary>
[Docable]
public class MenuPeriodCosmosModel : IPartitionedItem
{
    [DocIgnore] public string id { get; set; }
    public string BasicModelCode { get; set; }
    [DocIgnore] public string ItemType => ModelTypes.MenuPeriod;

    public long VariantID { get; set; }

    /// <summary>The link's own foreign key — what a service-interval fan-out finds this document by.</summary>
    public long ServiceIntervalID { get; set; }

    /// <summary>The interval, embedded and kept fresh by the service-interval fan-out.</summary>
    public ServiceIntervalCosmosModel ServiceInterval { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>
/// Labour time and consumable for one service-interval group within a variant — the variant-to-group
/// link, with the group's labour code and interval membership embedded. The membership is what decides
/// which periodic line a labour detail supplies.
/// </summary>
[Docable]
public class MenuLabourCosmosModel : IPartitionedItem
{
    [DocIgnore] public string id { get; set; }
    public string BasicModelCode { get; set; }
    [DocIgnore] public string ItemType => ModelTypes.MenuLabour;

    public long VariantID { get; set; }

    /// <summary>The link's own foreign key — what an interval-group fan-out finds this document by.</summary>
    public long ServiceIntervalGroupID { get; set; }

    public decimal AllowedTime { get; set; }

    /// <summary>UNSCALED — the transfer rate is applied at generation time, not here.</summary>
    public decimal Consumable { get; set; }

    /// <summary>The interval group, embedded and kept fresh by the interval-group fan-out.</summary>
    public ServiceIntervalGroupCosmosModel ServiceIntervalGroup { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>
/// A menu item — the variant-to-replacement-item link — with its parts and their country prices
/// EMBEDDED, since those rows are owned by the item and never read independently.
///
/// The replacement item, its standalone group and the interval groups it serves are embedded too. The
/// interval groups carry their full membership on purpose: generation asks "does group G contain
/// interval I" for EVERY group the item serves, including groups the variant has no labour detail for,
/// so the answer cannot be recovered from the sibling MenuLabour documents alone.
/// </summary>
[Docable]
public class MenuItemCosmosModel : IPartitionedItem
{
    [DocIgnore] public string id { get; set; }
    public string BasicModelCode { get; set; }
    [DocIgnore] public string ItemType => ModelTypes.MenuItem;

    public long MenuItemID { get; set; }
    public long VariantID { get; set; }
    public decimal StandaloneAllowedTime { get; set; }

    /// <summary>False when the item has no replacement-item link at all — such items generate nothing.</summary>
    public bool HasReplacementItem { get; set; }

    /// <summary>
    /// True when the LINK ROW itself (the replacement item applied to the vehicle model) is
    /// soft-deleted. Distinct from <c>ReplacementItem.IsDeleted</c>, which is the master row's flag.
    /// </summary>
    public bool ReplacementItemDeleted { get; set; }

    /// <summary>The replacement item, embedded and kept fresh by the replacement-item fan-out.</summary>
    public ReplacementItemCosmosModel ReplacementItem { get; set; }

    /// <summary>
    /// The interval groups the replacement item serves, each with its labour code and full interval
    /// membership. Kept fresh by both the interval-group and the replacement-item fan-outs.
    /// </summary>
    public List<ServiceIntervalGroupCosmosModel> ServiceIntervalGroups { get; set; } = [];

    /// <summary>Null when the item is ungrouped; otherwise the standalone group it folds into.</summary>
    public StandaloneReplacementItemGroupCosmosModel StandaloneGroup { get; set; }

    /// <summary>Carried unfiltered — the generator owns every part inclusion rule.</summary>
    public List<MenuItemPartCosmosModel> Parts { get; set; } = [];

    public bool IsDeleted { get; set; }
}

public class MenuItemPartCosmosModel
{
    public string PartNumber { get; set; }
    public int SortOrder { get; set; }
    public decimal? PeriodicQuantity { get; set; }
    public decimal? StandaloneQuantity { get; set; }
    public List<MenuPartCountryPriceCosmosModel> CountryPrices { get; set; } = [];
    public bool IsDeleted { get; set; }
}

public class MenuPartCountryPriceCosmosModel
{
    public long CountryID { get; set; }

    /// <summary>Dealer cost.</summary>
    public decimal? PartPrice { get; set; }

    /// <summary>Retail price.</summary>
    public decimal PartFinalPrice { get; set; }

    public bool IsDeleted { get; set; }
}
