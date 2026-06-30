using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains the sale information for a vehicle — the dealer, invoice details, warranty activation, broker, and end customer.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleSaleInformation
{
    /// <summary>
    /// The Country Hash ID where the vehicle was sold.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public string CountryID { get; set; }

    /// <summary>
    /// The resolved country name.
    /// </summary>
    public string CountryName { get; set; }

    /// <summary>
    /// The Company Hash ID of the selling dealer.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string CompanyID { get; set; }

    /// <summary>
    /// The resolved company (dealer) name.
    /// </summary>
    public string CompanyName { get; set; }

    /// <summary>
    /// The Branch Hash ID of the selling branch.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string BranchID { get; set; }

    /// <summary>
    /// The resolved branch name.
    /// </summary>
    public string BranchName { get; set; }

    /// <summary>
    /// The customer's account number at the dealer.
    /// </summary>
    public string CustomerAccountNumber { get; set; }

    /// <summary>
    /// The customer ID from the dealer's system.
    /// </summary>
    public string CustomerID { get; set; }

    /// <summary>
    /// The date on the sale invoice.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// The date the vehicle's warranty was activated.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyActivationDate { get; set; }

    /// <summary>
    /// The sale invoice number.
    /// </summary>
    public string InvoiceNumber { get; set; }

    [DocIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public decimal? InvoiceTotal { get; set; }

    [DocIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string Status { get; set; }

    [DocIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string Location { get; set; }

    [DocIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string SaleType { get; set; }

    [DocIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string AccountNumber { get; set; }

    /// <summary>
    /// The <see cref="VehicleBrokerSaleInformation">broker sale information</see> if the vehicle was sold through a broker.
    /// </summary>
    public VehicleBrokerSaleInformation Broker { get; set; }

    /// <summary>
    /// The <see cref="VehicleDistributorSaleInformation">distributor leg</see> of the supply chain, when the
    /// vehicle passed through a configured distributor before the dealer's sale. Null when there is no
    /// distributor leg (or none is configured). The distributor never makes the end-customer sale; this is
    /// informational context for the dealer sale above.
    /// </summary>
    public VehicleDistributorSaleInformation Distributor { get; set; }

    /// <summary>
    /// The <see cref="VehicleIntermediarySaleInformation">intermediary legs</see> (0..n) the vehicle passed
    /// through between the distributor and the dealer. Empty when there are none. Intermediaries never make
    /// the end-customer sale; this is informational context for the dealer sale above.
    /// </summary>
    public List<VehicleIntermediarySaleInformation> Intermediaries { get; set; } = new();

    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter]
    public string RegionID { get; set; }

    /// <summary>
    /// The <see cref="VehicleSaleEndCustomerInformationDTO">end customer information</see> for this sale.
    /// </summary>
    public VehicleSaleEndCustomerInformationDTO EndCustomer { get; set; }


    /// <summary>
    /// The City Hash ID from the Identity System.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CityHashIdConverter]
    public string CityID { get; set; }

    /// <summary>
    /// The resolved City Name.
    /// </summary>
    public string CityName { get; set; }

}