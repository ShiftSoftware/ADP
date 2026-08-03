using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Whether to attach the model's service menu to a vehicle lookup, and how to generate it. Supplied on
/// <see cref="VehicleLookupRequestOptions.ServiceMenuOptions"/>; set <see cref="Include"/> to turn the
/// section on, and everything else is optional.
///
/// <para>The menu's LANGUAGE is deliberately not here — it is
/// <see cref="VehicleLookupRequestOptions.LanguageCode"/>. A vehicle lookup rendering in one language with
/// menu codes generated in another would be a bug, so there is nowhere to express it.</para>
/// </summary>
[Docable]
public class VehicleServiceMenuRequestOptions
{
    /// <summary>
    /// Whether to include the model's service menu (<c>VehicleLookupDTO.ServiceMenu</c>) — the DMS menu
    /// codes, labour codes and prices for every service the vehicle's model offers. Off by default, so a
    /// caller that supplies this object for its other settings still has to ask for the section.
    ///
    /// <para>Deliberately per-request rather than per-deployment: the section costs an extra
    /// single-partition Cosmos read plus a fold PER VEHICLE, which a bulk lookup multiplies by its VIN
    /// count. Turn it on for the request that renders a menu, not for every request.</para>
    ///
    /// <para>Requires the service-menu lookup to be registered (<c>AddLookupService</c> does it) and its
    /// Cosmos containers to be provisioned; when either is missing the section reports why via
    /// <c>VehicleServiceMenuDTO.Status</c> rather than failing the lookup.</para>
    ///
    /// <para>A null <see cref="VehicleLookupRequestOptions.ServiceMenuOptions"/> and this left false mean
    /// the same thing — no menu. Two ways to say "off" is the cost of keeping the switch beside the
    /// settings it governs; what it buys is that a caller cannot configure the menu and then have those
    /// settings silently ignored because a flag on another object was missed.</para>
    /// </summary>
    public bool Include { get; set; }

    /// <summary>
    /// The country whose part prices and labour rate the menu is priced for. When null, the menu lookup's
    /// own <c>ServiceMenuLookupOptions.DefaultCountryID</c> applies, then 0 — which is the convention a
    /// deployment with no configured countries stores its prices under, not a magic value.
    /// </summary>
    public long? CountryID { get; set; }

    /// <summary>
    /// Scales the consumable charge on every generated line; 1 leaves it unscaled. When set, this WINS over
    /// the host's <c>ServiceMenuLookupOptions.CountrySettingsResolver</c> — it is the caller's explicit
    /// choice, and a value that is silently ignored is worse than one that is absent. When null the
    /// resolver applies, then 1.
    ///
    /// <para>This moves money: the consumable feeds each line's labour total and therefore the price quoted
    /// to a customer. It changes no menu or labour CODE — the labour-rate mapping is always keyed by the
    /// variant's primary rate — so the risk is confined to the figures. An endpoint that lets an untrusted
    /// caller set it is letting that caller move the quoted price; a host serving a public web component
    /// should fix the value server-side, or leave this null and configure the resolver, rather than binding
    /// it straight from a query string.</para>
    /// </summary>
    public decimal? TransferRate { get; set; }
}
