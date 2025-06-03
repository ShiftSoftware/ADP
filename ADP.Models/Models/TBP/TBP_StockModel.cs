using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_StockModel
{
    public string id { get; set; }

    /// <summary>
    /// First partition key
    /// </summary>
    public long BrandID { get; set; }

    /// <summary>
    /// Second partition key
    /// </summary>
    public long BrokerID { get; set; }

    /// <summary>
    /// Third partition key
    /// </summary>
    public string VIN { get; set; }

    public Brands Brand { get; set; }
    public int Quantity { get; set; }
    public DateTime? OneKOrFiveKServieDate { get; set; }
    public DateTime? FirstServieDate { get; set; }
    public TBP_BrokerModel? Broker { get; set; }
    public BrokerVehicleModel? Vehicle { get; set; }
    public IEnumerable<TBP_VehicleTransferModel> Transfers { get; set; }
    public IEnumerable<TBP_Invoice> Invoices { get; set; }
    public IEnumerable<TBP_VehicleEntryModel> VehilceEntries { get; set; }

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
                (Broker.AccountNumbers.Contains(x.CustomerAccountNumber) ? x.LineStatus.ToLower() == "i" && x.Status.ToLower() == "i" : true));

        if (stockBrandAccess?.TerminationDate is not null)
            Quantity = 0;
        else
            Quantity = (hasValidVSData == true ? 1 : 0) + (Invoices?.Any(x => !x.IsDeleted) ?? false ? -1 : 0) + transferBalance;
    }
}
