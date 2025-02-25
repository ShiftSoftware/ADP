using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleEntryModel : IPartitionedItem, IBrandProps, ICompanyProps, IBranchProps, IInvoiceLineProps
{
    public Brands Brand { get; set; }
    public string BrandIntegrationID { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? InvoiceNumber { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string id { get; set; }
    public VehicleModelModel VehicleModel { get; set; }
    public ExteriorColorModel ExteriorColor { get; set; }
    public InteriorColorModel InteriorColor { get; set; }
    public string RegionIntegrationId { get; set; }
    public string VIN { get; set; }
    public string SaleType { get; set; }
    public string ModelCode { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public string LineStatus { get; set; }
    public string Status { get; set; }
    public string LocationCode { get; set; }
    public string ExteriorColorCode { get; set; }
    public string InteriorColorCode { get; set; }
    public int LineNumber { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateInserted { get; set; }
    public PartitionedItemType ItemType => ModelTypes.VehicleEntry;
}