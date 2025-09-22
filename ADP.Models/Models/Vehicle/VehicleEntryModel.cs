using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleEntryModel : 
    IPartitionedItem, 
    IBrandProps, 
    ICompanyProps, 
    IRegionProps, 
    IBranchProps, 
    IOrderDocumentProps, 
    IInvoiceProps,
    IOrderLineProps
{
    public string id { get; set; }
    public Brands Brand { get; set; }
    public string BrandID { get; set; }
    public string BrandHashID { get; set; }
    public DateTime? ProductionDate { get; set; }
    public int? ModelYear { get; set; }
    public string ExteriorColorCode { get; set; }
    public string InteriorColorCode { get; set; }
    public ColorModel ExteriorColor { get; set; }
    public ColorModel InteriorColor { get; set; }
    public string ModelCode { get; set; }
    public string ModelDescription { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public string VIN { get; set; }
    public VehicleModelModel VehicleModel { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string BranchID { get; set; }
    public string BranchHashID { get; set; }
    public DateTime? WarrantyActivationDate { get; set; }
    public string InvoiceNumber { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public string RegionID { get; set; }
    public string RegionHashID { get; set; }
    public string SaleType { get; set; }
    public string OrderStatus { get; set; }
    public string Location { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
    public string LineID { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }

    /// <summary>
    /// Per Vehicle Service Item Eligibility
    /// </summary>
    public IEnumerable<string> EligibleServiceItemUniqueReferences { get; set; }
    public string ItemType => ModelTypes.VehicleEntry;


    public string OrderDocumentNumber { get; set; }
    public decimal? OrderQuantity { get; set; }
    public decimal? SoldQuantity { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string ItemStatus { get; set; }
    public string InvoiceStatus { get; set; }
}