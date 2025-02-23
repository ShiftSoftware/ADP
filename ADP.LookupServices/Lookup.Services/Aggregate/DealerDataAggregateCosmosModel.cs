using System.Collections.Generic;
using ShiftSoftware.ADP.Models.LookupCosmosModels;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Models.Aggregate;

public class DealerDataAggregateCosmosModel
{
    public List<VehicleEntryModel> VehicleEntries { get; set; }
    public List<InitialOfficialVINModel> InitialOfficialVINs { get; set; }
    public List<InvoiceModel> Invoices { get; set; }
    public List<InvoiceLaborLineModel> LaborLines { get; set; }
    public List<InvoicePartLineModel> PartLines { get; set; }
    public List<SSCAffectedVINModel> SSCAffectedVINs { get; set; }
    public List<WarrantyClaimModel> WarrantyClaims { get; set; }
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; }
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; }
    public List<PaidServiceInvoiceModel> PaidServiceInvoices { get; set; }
    public List<ServiceItemClaimLineModel> ServiceItemClaimLines { get; set; }
    public List<FreeServiceItemExcludedVINModel> ServiceItemExcludedVINs { get; set; }
    public List<FreeServiceItemDateShiftModel> FreeServiceItemDateShifts { get; set; }
    public List<WarrantyDateShiftCosmosModel> WarrantyDateShifts { get; set; }
    public PaintThicknessRecordModel PaintThicknessRecords { get; set; }
    public List<AccessoryModel> Accessories { get; set; }
}