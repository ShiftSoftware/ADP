using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleEntryModel : IPartitionedItem, IBrandProps, ICompanyProps, IRegionProps, IBranchProps, IInvoiceLineProps, IInvoiceProps
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
    public ColorModel ExteriorColor { get; set; }
    public ColorModel InteriorColor { get; set; }
    public string RegionIntegrationID { get; set; }
    public string VIN { get; set; }
    public string SaleType { get; set; }
    public string ModelCode { get; set; }
    public string ModelDescription { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public string LineStatus { get; set; }
    public string Status { get; set; }
    public string Location { get; set; }
    public string ExteriorColorCode { get; set; }
    public string InteriorColorCode { get; set; }
    public string LineID { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
    public string ItemType => ModelTypes.VehicleEntry;
}