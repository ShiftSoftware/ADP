using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// What to generate a service menu for. Everything except the basic model code is optional; the defaults come
/// from <see cref="ServiceMenuLookupOptions"/>, the feature's own options — not from
/// <see cref="LookupOptions"/>.
/// </summary>
[Docable]
public class ServiceMenuLookupRequest
{
    /// <summary>
    /// The model to generate for. This is the Cosmos partition key, so the lookup is a single-partition
    /// read. Matched exactly (after trimming) against the authored basic model code — no prefix or fuzzy
    /// matching, because a near-miss would serve another model's menu codes.
    /// </summary>
    public string BasicModelCode { get; set; }

    /// <summary>
    /// The country whose part prices and labour rate apply. When null, falls back to
    /// <see cref="ServiceMenuLookupOptions.DefaultCountryID"/> and then to 0 — which is the
    /// single-country deployment's own convention, not a magic value: a deployment with one country
    /// stores its prices under whatever id it uses, and one with none uses 0.
    /// </summary>
    public long? CountryID { get; set; }

    /// <summary>
    /// Language for the multi-language parts of a code (prefixes, postfixes, operation codes). A
    /// two-letter code or a culture name; null or empty means English. One request generates ONE
    /// language — call again to correlate another, matching lines by
    /// <see cref="ServiceMenuLineDTO.LineKey"/>.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Scales the consumable charge. When set, it WINS over
    /// <see cref="ServiceMenuLookupOptions.CountrySettingsResolver"/>; when null the resolver applies, then
    /// 1 (no scaling). Setting a value and silently getting a different one back is the worse failure, so an
    /// explicit choice is honoured — a host that wants the resolver to be the sole authority does not expose
    /// this to its callers. It moves money, never codes: the labour-rate mapping is always keyed by the
    /// variant's primary rate.
    /// </summary>
    public decimal? TransferRate { get; set; }

    // No variant filter, deliberately. A menu variant's id is a primary key in the menus database and
    // nothing outside it has one — a caller holds a VIN or a model code, never a variant id. Every live
    // variant of the model is returned and the caller picks from ServiceMenuVariantDTO (which carries
    // the id and the authored name). If a rule for "which variant applies to THIS vehicle" is ever
    // established, it belongs here as that rule — not as a list of ids no caller can populate.
}
