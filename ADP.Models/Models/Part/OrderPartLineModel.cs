using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// Refers to a Part Line on an Order (This might be a Job Card on Workshop Module, or a Counter Sale on Parts Module).
/// </summary>
[Docable]
public class OrderPartLineModel: 
    IPartitionedItem, 
    IBranchProps, 
    ICompanyProps, 
    IInvoiceProps, 
    IOrderDocumentProps, 
    IOrderLineProps,
    ICustomerProps
{
    [DocIgnore]
    public string id { get; set; } = default!;


    /// <summary>
    /// The unique identifier of the Order Line.
    /// </summary>
    public string LineID { get; set; }

    public string OrderStatus { get; set; }
    public string LoadStatus { get; set; }

    /// <summary>
    /// The date at which this line was invoiced.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }


    /// <summary>
    /// The Order Number associated with this line. (Job Card Number, Counter Sale Order Number)
    /// </summary>
    public string OrderDocumentNumber { get; set; }

    /// <summary>
    /// The Invoice Number associated with this part line.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// The Invoice <see cref="Currencies">Currency</see>
    /// </summary>
    public Currencies? InvoiceCurrency { get; set; }

    /// <summary>
    /// The quantity of the part line that sold.
    /// </summary>
    public decimal? SoldQuantity { get; set; }

    /// <summary>
    /// The quantity of the part line that ordered.
    /// </summary>
    public decimal? OrderQuantity { get; set; }

    /// <summary>
    /// The type of sale. (e.g. Internal, Bulk, Retail, etc.)
    /// </summary>
    public string SaleType { get; set; }


    /// <summary>   
    /// The Menu Code in case this part line is a menu item.
    /// </summary>
    public string MenuCode { get; set; }

    /// <summary>
    /// The final price of this line item after accounting for quantity, discounts, and any applicable taxes or additional charges
    /// </summary>
    public decimal? ExtendedPrice { get; set; }

    /// <summary>
    /// The uniqe Part Number of the <see cref="CatalogPartModel">Catalog Part</see>.
    /// </summary>
    public string PartNumber { get; set; }

    /// <summary>
    /// The Warehouse/Location Identifier where the transaction happens.
    /// </summary>
    public string Location { get; set; } = default!;


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
    /// The Vehicle Identification Number (VIN) of the vehicle associated with this part line.
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
    public string CompanyID { get; set; }


    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }


    [DocIgnore]
    public string BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }


    [DocIgnore]
    public string ItemType => ModelTypes.InvoicePartLine;
}
