using System.Collections.Generic;
using ShiftSoftware.ADP.Models.LookupCosmosModels;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class DealerDataAggregateCosmosModel
{
    public List<VSDataCosmosModel> VSData { get; set; }
    public List<TIQOfficialVINCosmosModel> TIQOfficialVIN { get; set; }
    public List<CPUCosmosModel> CPU { get; set; }
    public List<SOLaborCosmosModel> SOLabor { get; set; }
    public List<SOPartsCosmosModel> SOPart { get; set; }
    public List<TiqSSCAffectedVinCosmosModel> TiqSSCAffectedVin { get; set; }
    public List<ToyotaWarrantyClaimCosmosModel> ToyotaWarrantyClaim { get; set; }
    public List<BrokerInitialVehicleCosmosModel> BrokerInitialVehicle { get; set; }
    public List<BrokerInvoiceCosmosModel> BrokerInvoice { get; set; }
    public List<TiqExtendedWarrantySopCosmosModel> TiqExtendedWarrantySop { get; set; }
    public List<TLPPackageInvoiceCosmosModel> TLPPackageInvoice { get; set; }
    public List<ToyotaLoyaltyProgramTransactionLineCosmosModel> ToyotaLoyaltyProgramTransactionLine { get; set; }
    public List<TLPCancelledServiceItemCosmosModel> TLPCancelledServiceItem { get; set; }
    public List<VehicleFreeServiceExcludedVINsCosmosModel> VehicleFreeServiceExcludedVIN { get; set; }
    public List<VehicleFreeServiceInvoiceDateShiftVINsCosmosModel> VehicleFreeServiceInvoiceDateShiftVIN { get; set; }
    public List<WarrantyDateShiftCosmosModel> WarrantyShiftDate { get; set; }
    public PaintThicknessVehicleCosmosModel PaintThicknessVehicle { get; set; }
    public List<AccessoryCosmosModel> Accessories { get; set; }
}
