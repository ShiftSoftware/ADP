using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One row per vehicle in the top-level vehicle report: the lookup's headline fields, flattened into
/// columns. Text columns hold an empty string when the lookup has no value.
/// </summary>
[Docable]
public class VehicleLookupTopLevelReportModel
{
    public string VIN { get; set; }
    public bool IsAuthorized { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public Guid? SSCLogId { get; set; }
    public string BasicModelCode { get; set; }

    public string IdentifiersVin { get; set; }
    public string IdentifiersVariant { get; set; }
    public string IdentifiersKatashiki { get; set; }
    public string IdentifiersColor { get; set; }
    public string IdentifiersTrim { get; set; }
    public string IdentifiersBrandId { get; set; }

    public string SaleCountryId { get; set; }
    public string SaleCountryName { get; set; }
    public string SaleCompanyId { get; set; }
    public string SaleCompanyName { get; set; }
    public string SaleBranchId { get; set; }
    public string SaleBranchName { get; set; }
    public string SaleCustomerAccountNumber { get; set; }
    public string SaleCustomerId { get; set; }
    public DateTime? SaleInvoiceDate { get; set; }
    public DateTime? SaleWarrantyActivationDate { get; set; }
    public string SaleInvoiceNumber { get; set; }
    public string SaleRegionId { get; set; }

    public long? SaleBrokerId { get; set; }
    public string SaleBrokerName { get; set; }
    public long? SaleBrokerInvoiceNumber { get; set; }
    public DateTime? SaleBrokerInvoiceDate { get; set; }

    public string SaleEndCustomerId { get; set; }
    public string SaleEndCustomerName { get; set; }
    public string SaleEndCustomerPhone { get; set; }
    public string SaleEndCustomerIdNumber { get; set; }

    public bool WarrantyHasActiveWarranty { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public bool WarrantyActivationIsRequired { get; set; }
    public bool WarrantyHasExtendedWarranty { get; set; }
    public DateTime? WarrantyExtendedStartDate { get; set; }
    public DateTime? WarrantyExtendedEndDate { get; set; }
    public DateTime? WarrantyDistributorExtendedStartDate { get; set; }
    public DateTime? WarrantyDistributorExtendedEndDate { get; set; }
    public DateTime? WarrantyFreeServiceStartDate { get; set; }

    public string VariantInfoModelCode { get; set; }
    public string VariantInfoSfx { get; set; }
    public int? VariantInfoModelYear { get; set; }

    public string VehicleSpecModelCode { get; set; }
    public int? VehicleSpecModelYear { get; set; }
    public DateTime? VehicleSpecProductionDate { get; set; }
    public string VehicleSpecModelDescription { get; set; }
    public string VehicleSpecVariantDescription { get; set; }
    public string VehicleSpecClass { get; set; }
    public string VehicleSpecBodyType { get; set; }
    public string VehicleSpecEngine { get; set; }
    public string VehicleSpecCylinders { get; set; }
    public string VehicleSpecLightHeavyType { get; set; }
    public string VehicleSpecDoors { get; set; }
    public string VehicleSpecFuel { get; set; }
    public string VehicleSpecTransmission { get; set; }
    public string VehicleSpecSide { get; set; }
    public string VehicleSpecEngineType { get; set; }
    public string VehicleSpecTankCap { get; set; }
    public string VehicleSpecStyle { get; set; }
    public int? VehicleSpecFuelLiter { get; set; }
    public string VehicleSpecExteriorColor { get; set; }
    public string VehicleSpecInteriorColor { get; set; }

    /// <summary>
    /// The classification codes the lookup resolved, in its order, separated by a semicolon and a space.
    /// These are the stable values to filter on. Empty when the host resolves none or configures no classifier.
    /// </summary>
    public string ClassificationCodes { get; set; }

    /// <summary>
    /// The display labels of <see cref="ClassificationCodes"/>, in the same order and with the same
    /// separator. A classification without a label shows its code. Display text only; filter on the codes.
    /// </summary>
    public string ClassificationLabels { get; set; }
}
