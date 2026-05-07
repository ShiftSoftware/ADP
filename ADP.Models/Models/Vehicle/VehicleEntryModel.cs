using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a vehicle entry in the dealer stock. Each entry corresponds to a vehicle record from a dealer's system,
/// capturing vehicle specifications, sale/invoice information, and warranty activation details.
/// </summary>
[Docable]
public class VehicleEntryModel :
    IPartitionedItem,
    IBrandProps,
    ICompanyProps,
    ICountryProps,
    IRegionProps,
    IBranchProps,
    IOrderDocumentProps,
    IInvoiceProps,
    IOrderLineProps,
    ICompanyIntegrationProps,
    IBranchIntegrationProps
{
    [DocIgnore]
    public string id { get; set; }

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }

    /// <summary>
    /// The date the vehicle was produced by the manufacturer.
    /// </summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// The model year of the vehicle.
    /// </summary>
    public int? ModelYear { get; set; }

    /// <summary>
    /// The exterior color code of the vehicle.
    /// </summary>
    public string ExteriorColorCode { get; set; }

    /// <summary>
    /// The interior color code of the vehicle.
    /// </summary>
    public string InteriorColorCode { get; set; }

    /// <summary>
    /// The model code that identifies the vehicle model.
    /// </summary>
    public string ModelCode { get; set; }

    /// <summary>
    /// A human-readable description of the vehicle model.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// The Katashiki (model-specific identifier used by the manufacturer).
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The variant code of the vehicle within its model range.
    /// </summary>
    public string VariantCode { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN).
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The date on the sale invoice for this vehicle.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

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
    public long? CountryID { get; set; }

    /// <summary>
    /// The Country Hash ID from the Identity System.
    /// </summary>
    public string CountryHashID { get; set; }

    /// <summary>
    /// The date when the vehicle's warranty was activated. This may differ from the invoice date.
    /// </summary>
    public DateTime? WarrantyActivationDate { get; set; }

    /// <summary>
    /// The invoice number for the vehicle sale transaction.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// The dealer account number associated with this transaction.
    /// </summary>
    public string AccountNumber { get; set; }

    /// <summary>
    /// The customer's account number at the dealer.
    /// </summary>
    public string CustomerAccountNumber { get; set; }

    /// <summary>
    /// The customer ID associated with this vehicle sale.
    /// </summary>
    public string CustomerID { get; set; }

    /// <summary>
    /// The total amount on the sale invoice.
    /// </summary>
    public decimal? InvoiceTotal { get; set; }

    [DocIgnore]
    public long? RegionID { get; set; }

    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    public string RegionHashID { get; set; }

    /// <summary>
    /// The type of sale (e.g., Retail, Fleet, Internal).
    /// </summary>
    public string SaleType { get; set; }

    /// <summary>
    /// The current status of the order associated with this vehicle entry.
    /// </summary>
    public string OrderStatus { get; set; }

    /// <summary>
    /// The location or warehouse identifier where the vehicle is held.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The currency used on the sale invoice.
    /// </summary>
    public Currencies? InvoiceCurrency { get; set; }

    /// <summary>
    /// The line item identifier within the order document.
    /// </summary>
    public string LineID { get; set; }

    /// <summary>
    /// The date when this record was loaded into the system.
    /// </summary>
    public DateTime? LoadDate { get; set; }

    /// <summary>
    /// The date when this record was posted/finalized.
    /// </summary>
    public DateTime? PostDate { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.VehicleEntry;

    /// <summary>
    /// The order document number for this vehicle transaction.
    /// </summary>
    public string OrderDocumentNumber { get; set; }

    /// <summary>
    /// The quantity ordered.
    /// </summary>
    public decimal? OrderQuantity { get; set; }

    /// <summary>
    /// The quantity sold.
    /// </summary>
    public decimal? SoldQuantity { get; set; }

    /// <summary>
    /// The extended price (unit price multiplied by quantity).
    /// </summary>
    public decimal? ExtendedPrice { get; set; }

    /// <summary>
    /// The status of the line item (e.g., Open, Closed).
    /// </summary>
    public string ItemStatus { get; set; }

    /// <summary>
    /// The status of the invoice (e.g., Paid, Pending).
    /// </summary>
    public string InvoiceStatus { get; set; }

    /// <summary>
    /// An external identifier used for company-level system-to-system integration.
    /// </summary>
    public string CompanyIntegrationID { get; set; }

    /// <summary>
    /// An external identifier used for branch-level system-to-system integration.
    /// </summary>
    public string BranchIntegrationID { get; set; }
}