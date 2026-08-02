using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// The per-country generation settings a host controls: how the consumable is scaled, and whether the
/// labour rate on a line follows the country or the variant's primary rate.
///
/// This mirrors the DMS export's own country normalisation, which a menus host performs from its
/// configured country list — a deployment with zero or one configured country exports at transfer rate
/// 1 using the PRIMARY labour rate, and only a multi-country deployment uses per-country rates. The
/// lookup cannot see that configuration, so the host supplies the same answer here. Getting it wrong
/// does not change menu CODES (the labour-rate mapping is always keyed by the primary rate) — it
/// changes the money on the line.
/// </summary>
[Docable]
public class ServiceMenuCountrySettings
{
    /// <summary>Scales the consumable charge; the generator rounds the result to 2 decimals. Default 1.</summary>
    public decimal TransferRate { get; set; } = 1m;

    /// <summary>
    /// When true the variant's primary labour rate is charged and per-country labour rates are ignored.
    /// Affects only the rate ON the line — never the generated labour code.
    /// </summary>
    public bool UsePrimaryLabourRate { get; set; }
}
