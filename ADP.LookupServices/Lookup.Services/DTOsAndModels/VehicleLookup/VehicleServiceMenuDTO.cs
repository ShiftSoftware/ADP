using ShiftSoftware.ADP.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// The service menu offered for this vehicle's model: the DMS menu codes, labour codes and prices for every
/// service the model's menu offers, flattened into one list.
///
/// <para>Present only when the request asked for it
/// (<see cref="VehicleServiceMenuRequestOptions.Include"/>); otherwise
/// <see cref="VehicleLookupDTO.ServiceMenu"/> is null. It costs one extra single-partition Cosmos read plus an
/// in-memory fold per vehicle, so it is opt-in rather than always-on.</para>
///
/// <para><b>Read <see cref="Status"/> before reading <see cref="Services"/>.</b> An empty list means five
/// different things — no join key, no menu authored for the model, a menu that generates nothing, every
/// variant excluded by <see cref="VehicleServiceMenuRequestOptions.FreeFilter"/>, or a menu subsystem that
/// could not be consulted. The status separates the first, second and last; the filter you sent separates
/// the other two, which both arrive as <see cref="VehicleServiceMenuStatus.Found"/> with nothing in it. A
/// menu fault never fails the VIN lookup; it arrives here as
/// <see cref="VehicleServiceMenuStatus.Unavailable"/>.</para>
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceMenuDTO
{
    /// <summary>
    /// Whether a menu was found, and if not, why. Also the per-lookup signal a deployment counts to measure
    /// the derived-key hit rate (open item O3) — see <see cref="VehicleServiceMenuStatus"/>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleServiceMenuStatus Status { get; set; }

    /// <summary>
    /// The join key that was tried: the basic model code derived from the vehicle's Katashiki
    /// (<see cref="VehicleLookupDTO.BasicModelCode"/>), matched exactly against the authored menu code. Null
    /// when the vehicle has no Katashiki. Echoed even on a miss, because "which code did it look for" is the
    /// first question anyone asks about one.
    /// </summary>
    public string BasicModelCode { get; set; }

    /// <summary>
    /// The country the part prices and labour rate were resolved for. 0 unless a menu was actually generated.
    /// </summary>
    public long CountryID { get; set; }

    /// <summary>The language the codes and descriptions were generated in — the request's language code.</summary>
    public string Language { get; set; }

    /// <summary>
    /// The transfer rate the consumable was scaled by; 1 means unscaled. 0 unless a menu was actually
    /// generated.
    /// </summary>
    public decimal TransferRate { get; set; }

    /// <summary>
    /// Every service the model's menu offers, flat: per variant, the scheduled services in distance order
    /// followed by the standalone ones. Each line carries its variant, so a UI that wants the nested shape
    /// groups by <see cref="VehicleServiceMenuLineDTO.VariantID"/>. Empty whenever <see cref="Status"/> is not
    /// <see cref="VehicleServiceMenuStatus.Found"/> — and it can be empty even then, for a menu that exists
    /// but generates nothing.
    /// </summary>
    public List<VehicleServiceMenuLineDTO> Services { get; set; } = new List<VehicleServiceMenuLineDTO>();
}
