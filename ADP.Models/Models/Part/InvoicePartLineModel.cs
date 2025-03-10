using System;

namespace ShiftSoftware.ADP.Models.Part;

public class InvoicePartLineModel: IPartitionedItem, IBranchProps, ICompanyProps, IInvoiceProps, IInvoiceLineProps
{
    public string id { get; set; } = default!;
    public string Status { get; set; }
    public string LineStatus { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int JobNumber { get; set; }
    public int? InvoiceNumber { get; set; }
    public string InvoiceCurrency { get; set; }
    public decimal? Quantity { get; set; }
    public string SaleType { get; set; }
    public string AccountNumber { get; set; }
    public string MenuCode { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string PartNumber { get; set; }
    public string LineID { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string Department { get; set; }
    public string VIN { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
    public string CompanyID { get; set; }
    public string BranchID { get; set; }
    public string ItemType => ModelTypes.InvoicePartLine;
}
