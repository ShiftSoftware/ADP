using System;

namespace ShiftSoftware.ADP.Models.Part;

public class InvoicePartLineModel: IBranchProps, ICompanyProps, IInvoiceProps, IInvoiceLineProps
{
    public string id { get; set; } = default!;
    public string Status { get; set; }
    public string LineStatus { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int JobNumber { get; set; }
    public int? InvoiceNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string SaleType { get; set; }
    public string AccountNumber { get; set; }
    public string MenuCode { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string PartNumber { get; set; }
    public int LineNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string Department { get; set; }
    public string VIN { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateInserted { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public string ItemType => "Part";
}
