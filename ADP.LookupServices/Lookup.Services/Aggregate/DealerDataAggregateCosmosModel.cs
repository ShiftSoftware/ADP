using System.Collections.Generic;
using ShiftSoftware.ADP.Models.LookupCosmosModels;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Models.Aggregate;

public class DealerDataAggregateCosmosModel
{
    public List<VSDataCosmosModel> VSData { get; set; }
    public List<InitialOfficialVINModel> TIQOfficialVIN { get; set; }
    public List<InvoiceModel> CPU { get; set; }
    public List<InvoiceLaborLineModel> SOLabor { get; set; }
    public List<InvoicePartLineModel> SOPart { get; set; }
    public List<SSCAffectedVINModel> TiqSSCAffectedVin { get; set; }
    public List<WarrantyClaimModel> ToyotaWarrantyClaim { get; set; }
    public List<BrokerInitialVehicleModel> BrokerInitialVehicle { get; set; }
    public List<BrokerInvoiceModel> BrokerInvoice { get; set; }
    public List<PaidServiceInvoiceModel> TLPPackageInvoice { get; set; }
    public List<ToyotaLoyaltyProgramTransactionLineCosmosModel> ToyotaLoyaltyProgramTransactionLine { get; set; }
    public List<VehicleFreeServiceExcludedVINsCosmosModel> VehicleFreeServiceExcludedVIN { get; set; }
    public List<VehicleFreeServiceInvoiceDateShiftVINsCosmosModel> VehicleFreeServiceInvoiceDateShiftVIN { get; set; }
    public List<WarrantyDateShiftCosmosModel> WarrantyShiftDate { get; set; }
    public PaintThicknessVehicleModel PaintThicknessVehicle { get; set; }
    public List<AccessoryModel> Accessories { get; set; }
}
