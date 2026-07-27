namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// The per-run knobs of menu generation. One <see cref="MenuCodeGenerator.Generate"/> call produces
/// lines for ONE language and ONE country; a consumer that needs several runs the generator per
/// combination.
/// </summary>
public class MenuGenerationConfig
{
    /// <summary>
    /// Selects part prices and the country labour rate. A country with no matching rows is valid: part
    /// prices resolve to 0 and the labour rate falls back to the variant's primary rate.
    /// </summary>
    public long CountryID { get; set; }

    /// <summary>Scales the consumable (result rounded to 2 decimals). Defaults to 1 — no scaling.</summary>
    public decimal TransferRate { get; set; } = 1m;

    /// <summary>
    /// When true the variant's primary labour rate is used and country labour rates are ignored.
    /// Note this affects only the RATE ON THE LINE — the labour-rate mapping that feeds the labour
    /// CODE is always keyed by the primary rate regardless.
    /// </summary>
    public bool UsePrimaryLabourRate { get; set; }

    /// <summary>
    /// Language for multi-language values (prefixes, postfixes, operation codes, group menu codes).
    /// Accepts a two-letter code or a culture name; null or empty means English.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Whether to populate <see cref="GeneratedMenuPart.Cost"/> (dealer cost).
    ///
    /// DEFAULTS TO FALSE, deliberately. Dealer cost belongs to the DMS export only — it must never
    /// reach the vehicle lookup or a public web component. Making the SAFE case the default means a
    /// consumer leaks cost only by explicitly asking for it, never by forgetting to strip it.
    ///
    /// The DMS export sets this to true; the lookup leaves it alone.
    /// </summary>
    public bool IncludePartCost { get; set; }
}
