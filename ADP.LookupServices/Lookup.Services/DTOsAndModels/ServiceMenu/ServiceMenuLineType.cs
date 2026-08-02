using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// What produced a menu line. Mirrors the generator's own <c>MenuLineType</c>, so the distinction
/// between an individually-sold item and a folded item group survives into the lookup — the older
/// "is standalone" flag lost it.
/// </summary>
[Docable]
public enum ServiceMenuLineType
{
    /// <summary>A scheduled service at one interval.</summary>
    Periodic = 0,

    /// <summary>A standalone item sold on its own.</summary>
    StandaloneUngrouped = 1,

    /// <summary>A standalone group, folded from every item in it.</summary>
    StandaloneGrouped = 2,
}
