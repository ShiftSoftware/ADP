using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// The intermediary supply-chain leg embedded on a single <see cref="VehicleEntryModel"/>. Used by sources
/// that emit one entry per VIN and carry the supply chain inline, rather
/// than as a separate entry per leg. Represents a company (e.g. a regional importer) that moved the vehicle
/// between the distributor and the selling dealer. The intermediary never makes the end-customer sale.
/// <para>Null when the entry has no intermediary leg. Multi-entry sources (per-dealer DMS feeds) leave this
/// null and instead emit a distinct <see cref="VehicleEntryModel"/> for the intermediary, classified by
/// <c>LookupOptions.IsEndCustomerSale</c>.</para>
/// </summary>
[Docable]
public class IntermediarySaleLeg
{
    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>The Company Hash ID of the intermediary.</summary>
    public string CompanyHashID { get; set; }

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>The Branch Hash ID of the intermediary branch.</summary>
    public string BranchHashID { get; set; }

    /// <summary>The intermediary leg's invoice (sales-contract) number.</summary>
    public string InvoiceNumber { get; set; }

    /// <summary>The intermediary leg's invoice (sales-contract) date.</summary>
    public DateTime? InvoiceDate { get; set; }
}
