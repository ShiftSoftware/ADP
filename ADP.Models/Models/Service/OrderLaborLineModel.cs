using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Service;

/// <summary>
/// Refers to a Labor Line on an Order (Typically a Job Card on Workshop Module).
/// </summary>
[Docable]
public class OrderLaborLineModel : 
    IPartitionedItem, 
    IBranchProps, 
    ICompanyProps,
    IInvoiceProps,
    IOrderDocumentProps,
    IOrderLineProps,
    ICompanyIntegrationProps,
    IBranchIntegrationProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The unique identifier of the Labor Line.
    /// </summary>
    public string LineID { get; set; }

    /// <summary>
    /// The date at which this line was invoiced.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// The Order Number associated with this line. (Job Card Number, Counter Sale Order Number)
    /// </summary>
    public string OrderDocumentNumber { get; set; }

    /// <summary>
    /// A general description of the parent order/job associated with this labor line.
    /// </summary>
    public string JobDescription { get; set; }

    /// <summary>
    /// The Invoice Number associated with this labor line.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// The Parent Invoice Number associated with this labor line. (In case of Credit Notes or Debit Notes)
    /// </summary>
    public string ParentInvoiceNumber { get; set; }

    /// <summary>
    /// The Invoice <see cref="Currencies">Currency</see>
    /// </summary>
    public Currencies? InvoiceCurrency { get; set; }

    /// <summary>
    /// The quantity of the labor line that ordered.
    /// </summary>
    public decimal? OrderQuantity { get; set; }


    /// <summary>
    /// The quantity of the labor line that sold.
    /// </summary>
    public decimal? SoldQuantity { get; set; }

    /// <summary>
    /// The type of sale. (e.g. Internal, Bulk, Retail, etc.)
    /// </summary>
    public string SaleType { get; set; }

    /// <summary>   
    /// The Package Code in case this labor line is a package item.
    /// </summary>
    public string PackageCode { get; set; }


    /// <summary>
    /// The final price of this line item after accounting for quantity, discounts, and any applicable taxes or additional charges
    /// </summary>
    public decimal? ExtendedPrice { get; set; }

    /// <summary>
    /// The unique Labor/Operation Code. Typically from the Manufacturer's Service Catalog.
    /// </summary>
    public string LaborCode { get; set; }

    /// <summary>
    /// The service code associated with this line. Typically a distributor level code.
    /// </summary>
    public string ServiceCode { get; set; }


    /// <summary>
    /// The description associated with this line. Typically a free text or a distributor/dealer level description of the labor operation performed.
    /// </summary>
    public string ServiceDescription { get; set; }

    /// <summary>
    /// The Account Number from the Accounting System.
    /// </summary>
    public string AccountNumber { get; set; }

    /// <summary>
    /// The Customer Account Number from the Accounting System.
    /// </summary>
    public string CustomerAccountNumber { get; set; }

    /// <summary>
    /// The Company Specific Customer ID.
    /// </summary>
    public string CustomerID { get; set; }

    /// <summary>
    /// The Centralized unique Golden Customer ID.
    /// </summary>
    public string GoldenCustomerID { get; set; }


    /// <summary>
    /// The Department Code/ID.
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) of the vehicle associated with this labor line.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The date at which this line was loaded into the Order Document.
    /// </summary>
    public DateTime? LoadDate { get; set; }

    /// <summary>
    /// The date at which this line was posted. This could mean (Job Completed, Part Dispatched, Vehicle Allocated, etc. based on the type of the Order Document).
    /// </summary>
    public DateTime? PostDate { get; set; }


    [DocIgnore]
    public long? CompanyID { get; set; }


    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }


    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.InvoiceLaborLine;

    public string InvoiceStatus { get; set; }
    public string ItemStatus { get; set; }
    public string OrderStatus { get; set; }


    /// <summary>
    /// The date for the next scheduled service of the vehicle associated with this labor line.
    /// </summary>
    public DateTime? NextServiceDate { get; set; }

    /// <summary>
    /// The number of corresponding part lines associated with the parent order of this line.
    /// </summary>
    public int NumberOfPartLines { get; set; }


    /// <summary>
    /// An External Identifier that can be used for system to system Integration
    /// </summary>
    public string CompanyIntegrationID { get; set; }


    /// <summary>
    /// An External Identifier that can be used for system to system Integration
    /// </summary>
    public string BranchIntegrationID { get; set; }


    /// <summary>
    /// The odometer reading of the vehicle at the time this job was opened.
    /// </summary>
    public int? Odometer { get; set; }
}