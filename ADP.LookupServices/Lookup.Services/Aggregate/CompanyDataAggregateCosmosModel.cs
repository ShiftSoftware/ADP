using ShiftSoftware.ADP.Models.Invoice;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Aggregate;

public class CompanyDataAggregateCosmosModel
{
    public List<VehicleEntryModel> VehicleEntries { get; set; }
    public List<VehicleServiceActivation> VehicleServiceActivations { get; set; }
    public List<VehicleInspectionModel> VehicleInspections { get; set; }
    public List<InitialOfficialVINModel> InitialOfficialVINs { get; set; }
    public List<InvoiceModel> Invoices { get; set; }
    public List<InvoiceLaborLineModel> LaborLines { get; set; }
    public List<OrderPartLineModel> PartLines { get; set; }
    public List<SSCAffectedVINModel> SSCAffectedVINs { get; set; }
    public List<WarrantyClaimModel> WarrantyClaims { get; set; }
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; }
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; }
    public List<PaidServiceInvoiceModel> PaidServiceInvoices { get; set; }
    public List<ItemClaimModel> ItemClaims { get; set; }
    public List<FreeServiceItemExcludedVINModel> FreeServiceItemExcludedVINs { get; set; }
    public List<FreeServiceItemDateShiftModel> FreeServiceItemDateShifts { get; set; }
    public List<WarrantyDateShiftModel> WarrantyDateShifts { get; set; }
    public PaintThicknessInspectionModel PaintThicknessInspections { get; set; }
    public List<VehicleAccessoryModel> Accessories { get; set; }
}