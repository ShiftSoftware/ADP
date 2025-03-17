using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleBase
{
    public string id { get; set; }
    public Brands Brand { get; set; }
    public string BrandID { get; set; }
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
    public string BranchID { get; set; }
    public DateTime? WarrantyActivationDate { get; set; }
    public string InvoiceNumber { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public string RegionID { get; set; }
    public string SaleType { get; set; }
    public string Status { get; set; }
    public string Location { get; set; }
    public Currencies? InvoiceCurrency { get; set; }
}