using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Aggregate;

/// <summary>
/// The aggregate root model containing all company-related data for a vehicle (identified by VIN) from Cosmos DB.
/// This is the primary data structure passed to evaluators for processing vehicle lookups.
/// </summary>
[Docable]
public class CompanyDataAggregateModel
{
    /// <summary>The Vehicle Identification Number this aggregate belongs to.</summary>
    public string VIN { get; set; }
    /// <summary>All vehicle entries (sale records) for this VIN across companies.</summary>
    public List<VehicleEntryModel> VehicleEntries { get; set; } = new();
    /// <summary>Service activation records for this VIN.</summary>
    public List<VehicleServiceActivation> VehicleServiceActivations { get; set; } = new();
    /// <summary>Vehicle inspection records for this VIN.</summary>
    public List<VehicleInspectionModel> VehicleInspections { get; set; }  = new();
    /// <summary>Per-VIN campaign entries (ManualVinEntry trigger) for this VIN.</summary>
    public List<CampaignVinEntryModel> CampaignVinEntries { get; set; }  = new();
    /// <summary>Initial official VIN registration records.</summary>
    public List<InitialOfficialVINModel> InitialOfficialVINs { get; set; }  = new();
    /// <summary>Service history labor lines for this VIN.</summary>
    public List<OrderLaborLineModel> LaborLines { get; set; }  = new();
    /// <summary>Service history part lines for this VIN.</summary>
    public List<OrderPartLineModel> PartLines { get; set; }  = new();
    /// <summary>Special Service Campaign (SSC) / recall records for this VIN.</summary>
    public List<SSCAffectedVINModel> SSCAffectedVINs { get; set; }  = new();
    /// <summary>Warranty claim records for this VIN.</summary>
    public List<WarrantyClaimModel> WarrantyClaims { get; set; }  = new();
    /// <summary>Initial vehicle records from broker inventory for this VIN.</summary>
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; }  = new();
    /// <summary>Broker invoice records for this VIN.</summary>
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; }  = new();
    /// <summary>Paid service invoice records for this VIN.</summary>
    public List<PaidServiceInvoiceModel> PaidServiceInvoices { get; set; }  = new();
    /// <summary>Service item claims made for this VIN.</summary>
    public List<ItemClaimModel> ItemClaims { get; set; }  = new();
    /// <summary>VINs excluded from free service item campaigns.</summary>
    public List<FreeServiceItemExcludedVINModel> FreeServiceItemExcludedVINs { get; set; }  = new();
    /// <summary>Free service item date shift overrides for this VIN.</summary>
    public List<FreeServiceItemDateShiftModel> FreeServiceItemDateShifts { get; set; }  = new();
    /// <summary>Warranty date shift overrides for this VIN.</summary>
    public List<WarrantyDateShiftModel> WarrantyDateShifts { get; set; }  = new();
    /// <summary>Paint thickness inspection records for this VIN.</summary>
    public IEnumerable<PaintThicknessInspectionModel> PaintThicknessInspections { get; set; }
    /// <summary>Accessories installed on this vehicle.</summary>
    public List<VehicleAccessoryModel> Accessories { get; set; } = new();
    /// <summary>Extended warranty records for this VIN.</summary>
    public List<ExtendedWarrantyModel> ExtendedWarrantyEntries { get; set; } = new();
}