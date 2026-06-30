using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// The distributor leg of a vehicle's supply chain — the company that imported/distributed the vehicle to the
/// selling dealer. The distributor never makes the end-customer sale (see
/// <see cref="LookupOptions.IsEndCustomerSaleCompany"/>), so this block is purely informational and is
/// surfaced alongside the dealer's sale on <see cref="VehicleSaleInformation"/>. Null when the vehicle has no
/// distributor leg, or none is configured for the deployment.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleDistributorSaleInformation
{
    /// <summary>The Company Hash ID of the distributor.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string CompanyID { get; set; }

    /// <summary>The resolved distributor company name.</summary>
    public string CompanyName { get; set; }

    /// <summary>The Branch Hash ID of the distributor branch.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string BranchID { get; set; }

    /// <summary>The resolved distributor branch name.</summary>
    public string BranchName { get; set; }

    /// <summary>The distributor's invoice (or order) number for moving the vehicle toward the dealer.</summary>
    public string InvoiceNumber { get; set; }

    /// <summary>The date on the distributor's invoice — this supply-chain leg's key date.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    /// <summary>The City Hash ID from the Identity System.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CityHashIdConverter]
    public string CityID { get; set; }

    /// <summary>The resolved City Name.</summary>
    public string CityName { get; set; }
}
