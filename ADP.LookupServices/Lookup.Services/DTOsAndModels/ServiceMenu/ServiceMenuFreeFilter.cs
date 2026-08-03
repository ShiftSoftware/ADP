using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// Which menu variants a lookup should return, by the variant's "free of charge" flag.
///
/// <para>The filter selects VARIANTS, not lines: a variant is free or it is not, and every line it produces
/// inherits that. So a variant the filter excludes contributes nothing at all — not its scheduled services
/// and not its standalone ones.</para>
///
/// <para><b>It filters, it does not price.</b> The money on a free variant's lines is generated exactly as
/// it would be for any other variant; nothing here zeroes a total. Asking for
/// <see cref="FreeOnly"/> and rendering the prices you get back quotes a customer for a menu the catalog
/// calls free — read the flag on the result and render accordingly.</para>
///
/// <para>Filtering happens BEFORE generation, so an excluded variant costs nothing to skip. It also means an
/// excluded variant cannot fail: a variant whose reference data is missing would throw while generating, and
/// filtering it out first means it never does.</para>
///
/// <para>Zero is <see cref="All"/> so a default-constructed request filters nothing — a caller that never
/// heard of this option keeps the behaviour it had before the option existed.</para>
/// </summary>
[Docable]
public enum ServiceMenuFreeFilter
{
    /// <summary>Every variant, free or not. The default.</summary>
    All = 0,

    /// <summary>Only variants flagged free of charge.</summary>
    FreeOnly = 1,

    /// <summary>
    /// Only variants NOT flagged free of charge. Note this is the complement of <see cref="FreeOnly"/>
    /// over the model's variants, not "variants that cost something" — a not-free variant whose lines all
    /// price to 0 is still returned here.
    /// </summary>
    PaidOnly = 2,
}
