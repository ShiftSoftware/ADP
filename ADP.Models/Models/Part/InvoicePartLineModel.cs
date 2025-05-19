using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// The Invoice Part Line refers to the properties that define the details of each part line in an invoice.
/// </summary>
public class InvoicePartLineModel: IPartitionedItem, IBranchProps, ICompanyProps, IInvoiceProps, IInvoiceLineProps
{
    public string id { get; set; } = default!;
    public string Status { get; set; }
    public string LineStatus { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int JobNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
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
    public string CompanyHashID { get; set; }
    public string BranchID { get; set; }
    public string BranchHashID { get; set; }
    public string ItemType => ModelTypes.InvoicePartLine;
}
