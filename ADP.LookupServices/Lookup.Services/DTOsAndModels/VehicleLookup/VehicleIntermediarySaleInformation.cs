using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One intermediary leg of a vehicle's supply chain — a company (e.g. a regional importer) that moved the
/// vehicle between the distributor and the selling dealer. A vehicle can pass through more than one, so
/// <see cref="VehicleSaleInformation.Intermediaries"/> is a list. Like the distributor, an intermediary in this
/// role does not make the end-customer sale (see <see cref="LookupOptions.IsEndCustomerSale"/>); this block is
/// informational, surfaced alongside the dealer's sale.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleIntermediarySaleInformation
{
    /// <summary>The Company Hash ID of the intermediary.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string CompanyID { get; set; }

    /// <summary>The resolved intermediary company name.</summary>
    public string CompanyName { get; set; }

    /// <summary>The Branch Hash ID of the intermediary branch.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string BranchID { get; set; }

    /// <summary>The resolved intermediary branch name.</summary>
    public string BranchName { get; set; }

    /// <summary>The intermediary's invoice (or order) number for moving the vehicle toward the dealer.</summary>
    public string InvoiceNumber { get; set; }

    /// <summary>The date on the intermediary's invoice — this supply-chain leg's key date.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    /// <summary>The City Hash ID from the Identity System.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CityHashIdConverter]
    public string CityID { get; set; }

    /// <summary>The resolved City Name.</summary>
    public string CityName { get; set; }
}
