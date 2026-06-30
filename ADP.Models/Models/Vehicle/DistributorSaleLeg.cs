using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// The distributor supply-chain leg embedded on a single <see cref="VehicleEntryModel"/>, for sources that
/// emit one entry per VIN and carry the chain inline. Carries the
/// distributor's own sale to the dealer (a <b>direct</b> route) — its invoice number and date. The
/// distributor's identity comes from <c>LookupOptions.DistributorCompanyID</c> (it is implicit/configured,
/// not stored per vehicle), so only the contract fields live here.
/// <para>Null when the source carries no distributor sale for this vehicle — e.g. multi-entry feeds (the
/// distributor is a separate entry), or two-leg routes (the distributor's sale is to the intermediary and is
/// surfaced on the <see cref="IntermediarySaleLeg"/> instead).</para>
/// </summary>
[Docable]
public class DistributorSaleLeg
{
    /// <summary>The distributor leg's invoice (sales-contract) number.</summary>
    public string InvoiceNumber { get; set; }

    /// <summary>The distributor leg's invoice (sales-contract) date.</summary>
    public DateTime? InvoiceDate { get; set; }
}
