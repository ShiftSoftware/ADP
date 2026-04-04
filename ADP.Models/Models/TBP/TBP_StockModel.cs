using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker's stock record for a specific vehicle (VIN) under a specific brand.
/// Tracks the vehicle's quantity at the broker, service dates, transfers, invoices, and related vehicle entries.
/// </summary>
[Docable]
public class TBP_StockModel
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The brand ID (first partition key).
    /// </summary>
    [DocIgnore]
    public long BrandID { get; set; }

    /// <summary>
    /// The broker ID (second partition key).
    /// </summary>
    public long BrokerID { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (third partition key).
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The calculated stock quantity for this vehicle at the broker. Derived from vehicle entries, invoices, and transfers.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The date of the 1K or 5K service performed on this vehicle.
    /// </summary>
    public DateTime? OneKOrFiveKServieDate { get; set; }

    /// <summary>
    /// The date of the first service performed on this vehicle.
    /// </summary>
    public DateTime? FirstServieDate { get; set; }

    /// <summary>
    /// The <see cref="TBP_BrokerModel">broker</see> that holds this stock.
    /// </summary>
    public TBP_BrokerModel? Broker { get; set; }

    /// <summary>
    /// The <see cref="BrokerVehicleModel">vehicle</see> details for this stock record.
    /// </summary>
    public BrokerVehicleModel? Vehicle { get; set; }

    /// <summary>
    /// The <see cref="TBP_VehicleTransferModel">transfers</see> involving this vehicle at this broker.
    /// </summary>
    public IEnumerable<TBP_VehicleTransferModel> Transfers { get; set; }

    /// <summary>
    /// The <see cref="TBP_Invoice">invoices</see> for this vehicle at this broker.
    /// </summary>
    public IEnumerable<TBP_Invoice> Invoices { get; set; }

    /// <summary>
    /// The <see cref="TBP_VehicleEntryModel">vehicle entries</see> from the dealer system for this vehicle.
    /// </summary>
    public IEnumerable<TBP_VehicleEntryModel> VehilceEntries { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsAtStock
    {
        get
        {
            return Quantity > 0;
        }
    }

    public void CalculateQuantity()
    {
        var stockBrandAccess = Broker?.BrandAccesses?.FirstOrDefault(x => x.BrandID == BrandID);

        if(stockBrandAccess?.Active != true || Broker.IsDeleted)
        {
            Quantity = 0;
            return;
        }

        var transferBalance = Transfers?.Where(x => !x.IsDeleted).Sum(x =>
        {
            if (x.SellerBrokerID == BrokerID)
                return -1;
            else if (x.BuyerBrokerID == BrokerID)
                return 1;

            return 0;
        }) ?? 0;

        bool hasValidVSData = false;
        if (VehilceEntries?.Any() == true)
            hasValidVSData = VehilceEntries.Any(x => (stockBrandAccess?.AccountStartDate is not null && x.InvoiceDate.HasValue ? x.InvoiceDate >= stockBrandAccess.AccountStartDate : true) &&
                (Broker.AccountNumbers.Any(a => a.AccountNumber == x.CustomerAccountNumber && a.CompanyID == x.CompanyID) ? x.LineStatus.ToLower() == "i" && x.Status.ToLower() == "i" : true));

        if (stockBrandAccess?.TerminationDate is not null)
            Quantity = 0;
        else
            Quantity = (hasValidVSData == true ? 1 : 0) + (Invoices?.Any(x => !x.IsDeleted) ?? false ? -1 : 0) + transferBalance;
    }
}
