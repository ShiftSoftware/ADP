using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleServiceHistoryPartReportModel
{
    public string VIN { get; set; }
    public string ServiceType { get; set; }
    public DateTime? ServiceDate { get; set; }
    public int? Mileage { get; set; }
    public string CompanyName { get; set; }
    public string BranchName { get; set; }
    public string AccountNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public string ParentInvoiceNumber { get; set; }
    public string JobNumber { get; set; }

    public string PartNumber { get; set; }
    public decimal? PartQty { get; set; }
    public string PartPackageCode { get; set; }
    public string PartDescription { get; set; }
}
