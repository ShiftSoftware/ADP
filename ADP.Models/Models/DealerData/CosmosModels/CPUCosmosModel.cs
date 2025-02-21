using System;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class CPUCosmosModel
{
    public string id { get; set; } = default!;
    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public int? DealerId { get; set; }
    public string VIN { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string ServiceDetails { get; set; }
    public int? Mileage { get; set; }
    public int? WIPNo { get; set; }
    public string Account { get; set; }
    public int? Invoice { get; set; }
    public int? Branch { get; set; }
    public DateTime? next_service { get; set; }
}