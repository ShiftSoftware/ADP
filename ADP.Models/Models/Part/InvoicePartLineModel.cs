using System;

namespace ShiftSoftware.ADP.Models.Part;

public class InvoicePartLineModel
{
    public string id { get; set; } = default!;
    public string InvoiceStatus { get; set; }
    public string OrderStatus { get; set; }
    public DateTime DateLastEditted { get; set; }
    public int JobNumber { get; set; }
    public int InvoiceNumber { get; set; }
    public decimal? OrderQuantity { get; set; }
    public string SaleType { get; set; }
    public string AccountNnumber { get; set; }
    public string MenuCode { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string PartNumber { get; set; }
    public int LineNumber { get; set; }
    public int OriginalInvoiceNumber { get; set; }
    public string CustomerID { get; set; }
    public string Department { get; set; }
    public string VIN { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateInserted { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}
