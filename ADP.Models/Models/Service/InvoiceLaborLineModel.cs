using System;

namespace ShiftSoftware.ADP.Models.Service;

public class InvoiceLaborLineModel
{
    public string id { get; set; } = default!;
    public string VIN { get; set; }
    public string LaborCode { get; set; }
    public string InvoiceStatus { get; set; }
    public string LoadStatus { get; set; }
    public DateTime DateLastEditted { get; set; }
    public int JobNumber { get; set; }
    public int InvoiceNumber { get; set; }
    public string SaleType { get; set; }
    public string AccountNnumber { get; set; }
    public string MenuCode { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public int LineNumber { get; set; }
    public int OriginalInvoiceNumber { get; set; }
    public string CustomerID { get; set; }
    public string Department { get; set; }
    public string Description { get; set; }
    public string ServiceCode { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateInserted { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}