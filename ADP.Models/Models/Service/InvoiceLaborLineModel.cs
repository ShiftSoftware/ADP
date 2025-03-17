using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Service;

public class InvoiceLaborLineModel : IPartitionedItem, IBranchProps, ICompanyProps, IInvoiceProps, IInvoiceLineProps
{
    public string id { get; set; } = default!;
    public string VIN { get; set; }
    public string LaborCode { get; set; }
    public string Status { get; set; }
    public string LineStatus { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int JobNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
    public string SaleType { get; set; }
    public string AccountNumber { get; set; }
    public string MenuCode { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string CustomerID { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceDescription { get; set; }
    public string Department { get; set; }
    public string LineID { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
    public string CompanyID { get; set; }
    public string BranchID { get; set; }
    public string ItemType => ModelTypes.InvoiceLaborLine;
}