using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// The neutral input to <see cref="MenuCodeGenerator"/> — one model's worth of menu data plus the
/// reference data the generated codes depend on.
///
/// This is LAYER 1 of the design in COSMOS_REPLICATION_PLAN.md §1.1: it is deliberately free of EF
/// entities, Cosmos documents and report models. Each consumer aggregates its own data INTO this shape
/// and maps <see cref="GeneratedMenuLine"/> back OUT to its own models, so the only thing shared
/// between the DMS export and the vehicle lookup is the generation logic itself.
///
/// <b>This contains LIVE ROWS ONLY.</b> Every adapter filters soft-deleted rows out on the way in, so
/// nothing here carries an <c>IsDeleted</c> and the generator never reasons about deletion — it applies
/// menu rules to the data it is given. That keeps the generator about menu generation, and puts the
/// "what counts as deleted" question where the data is loaded, which is where it can also be pushed
/// down into the query.
///
/// The cost of that split is real and worth naming: the two adapters must agree, and nothing in the type
/// system makes them. What holds them together is
/// <c>ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted</c>, which soft-deletes one row of
/// each table in turn and asserts the export and the lookup still produce identical output. Add a table
/// to the replication and you must add it there too.
/// </summary>
public class MenuGenerationRequest
{
    /// <summary>
    /// The variants to generate for. Enumeration order is preserved in the output, so a consumer that
    /// needs stable output must supply a stable order.
    /// </summary>
    public List<MenuGenerationVariant> Variants { get; set; } = [];

    public MenuGenerationReferenceData Reference { get; set; } = new MenuGenerationReferenceData();
}

/// <summary>One menu variant and the child rows the fold walks.</summary>
public class MenuGenerationVariant
{
    public long VariantID { get; set; }

    /// <summary>From the parent menu — a component of every generated menu code.</summary>
    public string BasicModelCode { get; set; }

    /// <summary>From the parent menu's vehicle model. Selects the labour-rate and brand mappings.</summary>
    public long? BrandID { get; set; }

    /// <summary>From the parent menu's vehicle model — carried through to the line for reporting.</summary>
    public string Model { get; set; }

    public string VariantName { get; set; }

    /// <summary>Raw (possibly a multi-language JSON object) — resolved per language by the generator.</summary>
    public string MenuPrefix { get; set; }

    /// <summary>Raw (possibly a multi-language JSON object).</summary>
    public string MenuPostfix { get; set; }

    /// <summary>Raw (possibly a multi-language JSON object).</summary>
    public string StandaloneMenuPrefix { get; set; }

    /// <summary>Raw (possibly a multi-language JSON object).</summary>
    public string StandaloneMenuPostfix { get; set; }

    /// <summary>
    /// The variant's PRIMARY labour rate. This is the labour-rate-mapping lookup key (never the
    /// country-resolved rate), and separately the fallback when no country rate matches.
    /// </summary>
    public decimal LabourRate { get; set; }

    public decimal? DiscountPercentage { get; set; }

    /// <summary>When false the variant contributes no standalone lines at all.</summary>
    public bool HasStandaloneItems { get; set; }

    public List<MenuGenerationCountryLabourRate> CountryLabourRates { get; set; } = [];
    public List<MenuGenerationPeriod> Periods { get; set; } = [];
    public List<MenuGenerationLabour> Labours { get; set; } = [];
    public List<MenuGenerationItem> Items { get; set; } = [];
}

public class MenuGenerationCountryLabourRate
{
    public long CountryID { get; set; }
    public decimal LabourRate { get; set; }
}

/// <summary>A service interval the variant is periodically available for.</summary>
public class MenuGenerationPeriod
{
    public long ServiceIntervalID { get; set; }
}

/// <summary>Labour time and consumable for a service-interval GROUP.</summary>
public class MenuGenerationLabour
{
    public long ServiceIntervalGroupID { get; set; }
    public decimal AllowedTime { get; set; }

    /// <summary>UNSCALED — the generator multiplies by <see cref="MenuGenerationConfig.TransferRate"/>.</summary>
    public decimal Consumable { get; set; }
}

/// <summary>A menu item (a replacement item applied to this variant) and its parts.</summary>
public class MenuGenerationItem
{
    public long MenuItemID { get; set; }

    public decimal StandaloneAllowedTime { get; set; }

    /// <summary>
    /// The service-interval groups the underlying replacement item serves. A part joins a PERIODIC line
    /// when one of these groups contains that line's service interval.
    /// </summary>
    public List<long> ReplacementItemServiceIntervalGroupIDs { get; set; } = [];

    /// <summary>Raw (possibly a multi-language JSON object) — part of the ungrouped standalone code.</summary>
    public string StandaloneOperationCode { get; set; }

    public string StandaloneLabourCode { get; set; }

    /// <summary>Description of an ungrouped standalone line.</summary>
    public string FriendlyName { get; set; }

    /// <summary>Null when the item is ungrouped; set when it folds into a grouped standalone line.</summary>
    public MenuGenerationStandaloneGroup StandaloneGroup { get; set; }

    public List<MenuGenerationPart> Parts { get; set; } = [];
}

public class MenuGenerationStandaloneGroup
{
    public long ID { get; set; }

    /// <summary>Raw (possibly a multi-language JSON object) — part of the grouped standalone code.</summary>
    public string MenuCode { get; set; }

    public string LabourCode { get; set; }

    /// <summary>Description of a grouped standalone line.</summary>
    public string Name { get; set; }
}

public class MenuGenerationPart
{
    public string PartNumber { get; set; }

    /// <summary>Authored order within the item. Carried through to the result; not used for sorting.</summary>
    public int SortOrder { get; set; }

    /// <summary>Quantity on a periodic line. Null or non-positive excludes the part from periodic lines.</summary>
    public decimal? PeriodicQuantity { get; set; }

    /// <summary>Quantity on a standalone line. Null or non-positive excludes the part from standalone lines.</summary>
    public decimal? StandaloneQuantity { get; set; }

    public List<MenuGenerationPartPrice> CountryPrices { get; set; } = [];
}

public class MenuGenerationPartPrice
{
    public long CountryID { get; set; }

    /// <summary>Dealer cost.</summary>
    public decimal? PartPrice { get; set; }

    /// <summary>Retail price.</summary>
    public decimal PartFinalPrice { get; set; }
}

/// <summary>
/// Shared reference data. In the export these come from the menus database; in the lookup they come
/// from the cached reference partition. Either way the generator only sees resolved dictionaries.
/// </summary>
public class MenuGenerationReferenceData
{
    public IReadOnlyDictionary<long, MenuGenerationServiceInterval> Intervals { get; set; } = new Dictionary<long, MenuGenerationServiceInterval>();

    public IReadOnlyDictionary<long, MenuGenerationServiceIntervalGroup> Groups { get; set; } = new Dictionary<long, MenuGenerationServiceIntervalGroup>();

    /// <summary>(brand, primary labour rate) → labour-rate code. See <see cref="MenuGenerationLabourRateKey"/>.</summary>
    public IReadOnlyDictionary<MenuGenerationLabourRateKey, string> LabourRateCodes { get; set; } = new Dictionary<MenuGenerationLabourRateKey, string>();

    /// <summary>
    /// Brand → its mapping. A brand with no entry still generates: the abbreviation falls back to "Z"
    /// and the company code is left null.
    /// </summary>
    public IReadOnlyDictionary<long?, MenuGenerationBrandMapping> BrandMappings { get; set; } = new Dictionary<long?, MenuGenerationBrandMapping>();
}

/// <summary>
/// A brand's mapping. Two distinct codes: the <see cref="Abbreviation"/> is concatenated into the
/// labour code, while the <see cref="Code"/> is the company code a DMS export writes as its own
/// column. Both are surfaced on the generated line so consumers need no mapping dictionary of their own.
/// </summary>
public class MenuGenerationBrandMapping
{
    /// <summary>The company code. Not used in any generated code — carried through for consumers.</summary>
    public string Code { get; set; }

    /// <summary>Trailing component of every labour code. Falls back to "Z" when the brand is unmapped.</summary>
    public string Abbreviation { get; set; }
}

public class MenuGenerationServiceInterval
{
    public string Code { get; set; }

    /// <summary>
    /// Used verbatim as a periodic line's description — NOT language-resolved, matching the source
    /// behaviour. A multi-language value stored here surfaces raw.
    /// </summary>
    public string Description { get; set; }

    public int ValueInMeter { get; set; }
    public long GroupID { get; set; }
}

public class MenuGenerationServiceIntervalGroup
{
    /// <summary>Leading component of a periodic line's labour code.</summary>
    public string LabourCode { get; set; }

    public HashSet<long> ServiceIntervalIDs { get; set; } = [];
}

/// <summary>
/// Composite key for the labour-rate mapping: (brand, primary labour rate).
///
/// A struct rather than a formatted string ON PURPOSE — the source keys this lookup by
/// <c>decimal</c> equality, under which <c>12.5</c> and <c>12.50</c> are the SAME key. A string key
/// would treat them as different and silently miss mappings that the export resolves.
/// </summary>
public readonly struct MenuGenerationLabourRateKey : IEquatable<MenuGenerationLabourRateKey>
{
    public long? BrandID { get; }
    public decimal LabourRate { get; }

    public MenuGenerationLabourRateKey(long? brandId, decimal labourRate)
    {
        BrandID = brandId;
        LabourRate = labourRate;
    }

    public bool Equals(MenuGenerationLabourRateKey other) =>
        Nullable.Equals(BrandID, other.BrandID) && LabourRate == other.LabourRate;

    public override bool Equals(object obj) => obj is MenuGenerationLabourRateKey other && Equals(other);

    public override int GetHashCode()
    {
        // decimal.GetHashCode is value-normalised, so 12.5 and 12.50 hash alike — required for the
        // scale-insensitive equality above to work in a dictionary.
        var brandHash = BrandID.HasValue ? BrandID.Value.GetHashCode() : 0;
        return brandHash ^ LabourRate.GetHashCode();
    }

    public override string ToString() => $"{BrandID}|{LabourRate}";
}
