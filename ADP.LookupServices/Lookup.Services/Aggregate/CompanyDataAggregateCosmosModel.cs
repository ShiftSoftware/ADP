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
    public List<VehicleInspectionModel> VehicleInspections { get; set; } = new();
    public List<InitialOfficialVINModel> InitialOfficialVINs { get; set; } = new();
    public List<InvoiceModel> Invoices { get; set; } = new();
    public List<InvoiceLaborLineModel> LaborLines { get; set; } = new();
    public List<InvoicePartLineModel> PartLines { get; set; } = new();
    public List<SSCAffectedVINModel> SSCAffectedVINs { get; set; } = new();
    public List<WarrantyClaimModel> WarrantyClaims { get; set; } = new();
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; } = new();
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; } = new();
    public List<PaidServiceInvoiceModel> PaidServiceInvoices { get; set; } = new();
    public List<ServiceItemClaimLineModel> ServiceItemClaimLines { get; set; } = new();
    public List<FreeServiceItemExcludedVINModel> FreeServiceItemExcludedVINs { get; set; } = new();
    public List<FreeServiceItemDateShiftModel> FreeServiceItemDateShifts { get; set; } = new();
    public List<WarrantyDateShiftModel> WarrantyDateShifts { get; set; } = new();
    public List<VehicleAccessoryModel> Accessories { get; set; } = new();
    public PaintThicknessInspectionModel PaintThicknessInspections { get; set; } = new();
}