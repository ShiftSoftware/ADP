using ShiftSoftware.ADP.Models.Invoice;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Aggregate;

public class CompanyDataAggregateCosmosModel
{
    public List<VehicleEntryModel> VehicleEntries { get; set; } = new();
    public List<VehicleServiceActivation> VehicleServiceActivations { get; set; } = new();
    public List<VehicleInspectionModel> VehicleInspections { get; set; }
    public List<InitialOfficialVINModel> InitialOfficialVINs { get; set; } = new();
    public List<InvoiceModel> Invoices { get; set; }
    public List<OrderLaborLineModel> LaborLines { get; set; }
    public List<OrderPartLineModel> PartLines { get; set; } = new();
    public List<SSCAffectedVINModel> SSCAffectedVINs { get; set; } = new();
    public List<WarrantyClaimModel> WarrantyClaims { get; set; } = new();
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