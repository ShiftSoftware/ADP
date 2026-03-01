using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleSscReportModel
{
    public string VIN { get; set; }
    public string SSCCode { get; set; }
    public string Description { get; set; }

    public bool Repaired { get; set; }
    public DateTime? RepairDate { get; set; }

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
