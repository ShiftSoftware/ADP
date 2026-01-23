using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Service;

public class OrderLaborLineModel : 
    IPartitionedItem, 
    IBranchProps, 
    ICompanyProps,
    IInvoiceProps,
    IOrderDocumentProps,
    IOrderLineProps
{
    public string id { get; set; } = default!;
    public string VIN { get; set; }
    public string LaborCode { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string OrderDocumentNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public string ParentInvoiceNumber { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
    public string SaleType { get; set; }
    public string AccountNumber { get; set; }
    public string PackageCode { get; set; }
    public string CustomerID { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceDescription { get; set; }
    public string Department { get; set; }
    public string LineID { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }
    public string ItemType => ModelTypes.InvoiceLaborLine;

    public string InvoiceStatus { get; set; }
    public decimal? OrderQuantity { get; set; }
    public decimal? SoldQuantity { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string ItemStatus { get; set; }
    public string OrderStatus { get; set; }

    /// <summary>
    /// The description of the parent job associated with this labor line.
    /// </summary>
    public string JobDescription { get; set; }

    /// <summary>
    /// The date for the next scheduled service of the vehicle associated with this labor line.
    /// </summary>
    public DateTime? NextServiceDate { get; set; }

    /// <summary>
    /// The number of corresponding part lines associated with the parent order of this line.
    /// </summary>
    public int NumberOfPartLines { get; set; }
}