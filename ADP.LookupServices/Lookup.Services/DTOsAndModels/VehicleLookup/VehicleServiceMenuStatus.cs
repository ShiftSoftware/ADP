using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Why the vehicle's service menu section holds what it holds.
///
/// <para>The vehicle lookup joins to the menu catalog on a <b>derived</b> key — the basic model code the
/// vehicle's Katashiki reduces to — against a code an author typed into the menus database. That join can
/// miss, and a miss looks exactly like a provisioning fault or an empty catalog if all three collapse into
/// "no menu". They do not collapse here: each one is its own value, so a deployment can count them.</para>
///
/// <para><b>This is the instrument for open item O3</b> (COSMOS_REPLICATION_PLAN.md §11) — the
/// derived-Katashiki vs authored-basic-model-code hit rate. Log this per lookup and the hit rate is
/// <c>Found / (Found + NotFound)</c>; <see cref="NoBasicModelCode"/> is not a miss, it is a vehicle that
/// never had a key to join on.</para>
///
/// <para>Zero is deliberately not <see cref="Found"/>: a default-constructed section must not claim to have
/// found a menu.</para>
/// </summary>
[Docable]
public enum VehicleServiceMenuStatus
{
    /// <summary>
    /// The vehicle carries no Katashiki, so no basic model code could be derived and no lookup was
    /// attempted. Not a miss — there was nothing to join on.
    /// </summary>
    NoBasicModelCode = 0,

    /// <summary>
    /// A basic model code was derived, and no menu is replicated under it. Either nobody authored a menu
    /// for this model, or the derived code does not match the authored one — the two are indistinguishable
    /// from here, and together they are the O3 miss rate.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// A menu was found for the derived code. <c>Services</c> can still be empty: a menu whose every
    /// variant is deleted, or whose intervals carry no labour details, exists and generates nothing — a
    /// different thing from having no menu, and usually rendered differently.
    /// </summary>
    Found = 2,

    /// <summary>
    /// The menu lookup could not be consulted — the Cosmos containers are not provisioned, the documents
    /// reference master data they do not carry, or the read itself failed. The vehicle lookup succeeds
    /// anyway; only this section is missing. Remediation is in the menus host: provision with
    /// <c>MenuCosmosProvisioning.EnsureContainersAsync</c>, then run a full catch-up sweep.
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// The host never registered the service-menu lookup, so there is nothing to ask. Distinct from
    /// <see cref="Unavailable"/> because the fix is different: call <c>AddServiceMenuLookup</c> (or use
    /// <c>AddLookupService</c>, which now does) rather than provisioning anything.
    /// </summary>
    NotRegistered = 4,
}
