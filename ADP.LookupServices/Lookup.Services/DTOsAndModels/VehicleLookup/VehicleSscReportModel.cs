using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleSscReportModel
{
    public string VIN { get; set; }
    public string SSCCode { get; set; }
    public string Description { get; set; }

    public bool Repaired { get; set; }
    public DateTime? RepairDate { get; set; }
    /// <summary>Which evidence decided the repair: <c>SSCRecord</c>, <c>WarrantyClaim</c>, <c>ServiceHistory</c>, or <c>None</c> while the campaign is open.</summary>
    public string RepairSource { get; set; }

    /// <summary>The dealer and branch that carried out the repair, from the warranty claim or labor line that decided it. Empty otherwise.</summary>
    public string RepairCompanyId { get; set; }
    public string RepairCompanyName { get; set; }
    public string RepairBranchId { get; set; }
    public string RepairBranchName { get; set; }

    /// <summary>
    /// The vehicle's owner and warranty activation, from its sale information: the same values as the Sale columns of
    /// the top-level report. Empty for a vehicle without a vehicle entry.
    /// </summary>
    public string SaleCountryId { get; set; }
    public string SaleCountryName { get; set; }
    public string SaleCompanyId { get; set; }
    public string SaleCompanyName { get; set; }
    public string SaleBranchId { get; set; }
    public string SaleBranchName { get; set; }
    public DateTime? SaleWarrantyActivationDate { get; set; }

    public string LaborCode1 { get; set; }
    public string LaborDescription1 { get; set; }
    public decimal? LaborAllowedTime1 { get; set; }

    public string LaborCode2 { get; set; }
    public string LaborDescription2 { get; set; }
    public decimal? LaborAllowedTime2 { get; set; }

    public string LaborCode3 { get; set; }
    public string LaborDescription3 { get; set; }
    public decimal? LaborAllowedTime3 { get; set; }

    public string PartNumber1 { get; set; }
    public string PartDescription1 { get; set; }
    public bool? PartIsAvailable1 { get; set; }

    public string PartNumber2 { get; set; }
    public string PartDescription2 { get; set; }
    public bool? PartIsAvailable2 { get; set; }

    public string PartNumber3 { get; set; }
    public string PartDescription3 { get; set; }
    public bool? PartIsAvailable3 { get; set; }
}
