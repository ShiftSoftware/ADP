using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Invoice;


/// <summary>
/// A Service Invoice or a Counter Sale Parts Invoice.
/// </summary>
[Docable]
public class InvoiceModel: 
    IPartitionedItem, 
    ICompanyProps, 
    IBranchProps, 
    IInvoiceProps, 
    IOrderDocumentProps
{
    public string id { get; set; } = default!;
    public string OrderDocumentNumber { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }
    public string VIN { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string ServiceDetails { get; set; }
    public int? Mileage { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string SaleType { get; set; }
    public string InvoiceNumber { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
    public int? LaborLineCount { get; set; }
    public int? PartLineCount { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public string ItemType => ModelTypes.Invoice;

    public string InvoiceStatus { get; set; }
}