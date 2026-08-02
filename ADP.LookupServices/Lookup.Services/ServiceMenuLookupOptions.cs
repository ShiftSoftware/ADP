using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Models;
using System;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>
/// Configuration for the service-menu lookup, registered with
/// <c>AddServiceMenuLookup</c> and injected as <c>IOptions&lt;ServiceMenuLookupOptions&gt;</c>.
///
/// <para>Separate from <see cref="LookupOptions"/> on purpose. The menu lookup is a self-contained
/// feature over its own Cosmos containers: a host can want service menus without the vehicle lookup, or
/// the vehicle lookup without menus. Folding these three settings into the general options would make
/// every host carry them, and would tie turning menus on to the whole lookup registration.</para>
///
/// <para>None of the resolvers here take an <see cref="IServiceProvider"/>. A host that needs its own
/// services inside one configures the option with them instead, which is what the options pattern is
/// for:</para>
/// <code>
/// services.AddOptions&lt;ServiceMenuLookupOptions&gt;()
///         .Configure&lt;ICountryProvider&gt;((options, countries) =>
///             options.CountrySettingsResolver = async countryID => …);
/// </code>
/// </summary>
[Docable]
public class ServiceMenuLookupOptions
{
    /// <summary>
    /// Optional suffix appended to the platform-standard Cosmos database name for menu reads (e.g.
    /// "-alt" resolves "Services" as "Services-alt"). Intended for shared-emulator dev scenarios where
    /// more than one projection set coexists on one local emulator; a production deployment has its own
    /// Cosmos account and keeps the standard names (leave unset).
    /// </summary>
    public string CosmosDatabaseNameSuffix { get; set; }

    /// <summary>
    /// The country used when a request does not name one. A deployment serving a single country sets
    /// this once instead of threading it through every call. When neither this nor the request supplies
    /// a country, 0 is used — what a deployment with no configured countries stores its prices under.
    /// </summary>
    public long? DefaultCountryID { get; set; }

    /// <summary>
    /// Resolves the transfer rate and labour-rate mode for the country being looked up.
    ///
    /// <para>This exists because the DMS export normalises the same two settings from the menus host's
    /// configured country list — a deployment with zero or one country exports at transfer rate 1 using
    /// the variant's PRIMARY labour rate, and only a multi-country deployment charges per-country rates.
    /// That configuration lives in the menus host, not in Cosmos, so the lookup cannot read it; wiring
    /// this resolver is how a host makes the lookup's money match its own export's.</para>
    ///
    /// <para>When unset, the request's transfer rate is used (defaulting to 1) and per-country labour
    /// rates apply. That is correct for a multi-country deployment — but a single-country deployment
    /// that leaves it unset will charge a country labour rate where its export charges the primary one.
    /// <b>Generated menu and labour codes are unaffected either way</b>: the labour-rate mapping is
    /// always keyed by the primary rate.</para>
    /// </summary>
    public Func<long, ValueTask<ServiceMenuCountrySettings>> CountrySettingsResolver { get; set; }
}
