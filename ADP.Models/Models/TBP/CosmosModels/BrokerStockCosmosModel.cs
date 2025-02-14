using System.Collections.Generic;
using System.Linq;
using ShiftSoftware.ADP.Models.DealerData.CosmosModels;
using ShiftSoftware.ADP.Models.DTOs.TBP;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

namespace ShiftSoftware.ADP.Models.TBP.CosmosModels;

public class BrokerStockCosmosModel
{
    public string id { get; set; }
    public long BrokerId { get; set; }
    public string VIN { get; set; }
    public string ItemType => "BrokerStock";

    public string BrokerName { get; set; }

    public TBPBrokerCosmosModel? Broker { get; set; }
    public List<VSDataCosmosModel>? VSDatas { get; set; }
    public List<BrokerInvoiceCosmosModel>? Invoices { get; set; }
    public List<BrokerVehicleTransferCosmosModel>? Transfers { get; set; }

    public BrokerVehicleStockDTO Info { get; private set; }

    public int Quantity { get; private set; }

    public void Calculate()
    {
        var transferBalance = Transfers?.Where(x => !x.Deleted).Sum(x =>
        {
            if (x.BrokerId == BrokerId)
                return -1;
            else if (x.SoldToBrokerId == BrokerId)
                return 1;

            return 0;
        }) ?? 0;

        bool hasValidVSData = false;
        if (VSDatas?.Any() == true)
            hasValidVSData = VSDatas.Any(x => (Broker.AccountStartDate.HasValue && x.InvoiceDate.HasValue ? x.InvoiceDate >= Broker.AccountStartDate : true) &&
                (Broker.AccountNumbers.Contains(x.CustomerAccount) ? x.ACSStatus.ToLower() == "i" && x.ProgressCode.ToLower() == "i" : true));

        if (Broker?.TerminationDate is not null)
            Quantity = 0;
        else
            Quantity = (hasValidVSData == true ? 1 : 0) + (Invoices?.Any(x => !x.Deleted) ?? false ? -1 : 0) + transferBalance;

        var vs = VSDatas?.OrderByDescending(x=> x.InvoiceDate).FirstOrDefault();
        var transfer = Transfers?.Where(x=> !x.Deleted).OrderByDescending(x => x.TransferDate).FirstOrDefault();

        if (vs is not null)
        {
            var variantInfo = Utility.ParseVariantInfo(vs.VariantCode);

            Info = new BrokerVehicleStockDTO
            {
                InvoiceDate = vs.InvoiceDate,
                AccountNumber = vs.CustomerAccount,
                BrokerName = BrokerName,
                Color = vs.Color,
                ColorDesc = vs.VTColor?.Color_Desc,
                Cylinders = vs.VTModel?.Cylinders,
                Engine = vs.VTModel?.Engine,
                Katashiki = vs.VTModel?.Katashiki,
                Model = vs.VTModel?.Model_Desc,
                ModelYear = variantInfo.ModelYear.ToString(),
                MSP = 0,
                RSP = 0,
                TransferedFrom = transfer?.SoldToBrokerId == BrokerId ? transfer?.BrokerName : "",
                Trim = vs.Trim,
                TrimDesc = vs.VTTrim?.Trim_Desc,
                Variant_Code = vs.VariantCode,
                Variant_Desc = vs.VTModel?.Variant_Desc,
                VIN = vs.VIN,
                VSRegion = vs.Region
            };
        }
        else if(transfer is not null)
        {
            Info = new BrokerVehicleStockDTO
            {
                AccountNumber = "",
                BrokerName = BrokerName,
                Color = transfer.Color,
                ColorDesc = transfer.ColorDesc,
                Cylinders = transfer.Cylinders,
                Engine = transfer.Engine,
                Katashiki = transfer.Katashiki,
                Model = transfer.Model,
                ModelYear = transfer.ModelYear,
                MSP = 0,
                RSP = 0,
                InvoiceDate = transfer.TransferDate,
                TransferedFrom = transfer.SoldToBrokerId == BrokerId ? transfer.BrokerName : "",
                Trim = transfer.Trim,
                TrimDesc = transfer.TrimDesc,
                Variant_Code = transfer.Variant_Code,
                Variant_Desc = transfer.Variant_Desc,
                VIN = transfer.VIN,
                VSRegion = transfer.VSRegion,
                PlateNumber = transfer.PlateNumber,
                PlateNumberRegion = transfer.PlateNumberRegion
            };
        }
    }
}
