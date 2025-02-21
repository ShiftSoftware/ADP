using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.FranchiseData.CosmosModels;
using System;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class VSDataCosmosModel : IDealerDataModel
{
    public string id { get; set; } = default!;
    public VTModelRecordsCosmosModel VTModel { get; set; }
    public VTColorCosmosModel VTColor { get; set; }
    public VTTrimCosmosModel VTTrim { get; set; }
    public Franchises Brand { get; set; }
    public string BrandIntegrationID { get; set; }
    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public string RegionIntegrationId { get; set; }
    public int VSData_DealerId { get; set; }
    public string VIN { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string SellingBranch { get; set; }
    public string SaleType { get; set; }
    public int? SalesInvoiceNumber { get; set; }
    public string ModelCode { get; set; }
    public string Katashiki { get; set; }
    public string VariantCode { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public string ProgressCode { get; set; }
    public string InvoiceAccount { get; set; }
    public string ACSStatus { get; set; }
    public string CustomerAccount { get; set; }
    public string Color { get; set; }
    public string Trim { get; set; }
    public string LocationCode { get; set; }
    public string Franchise { get; set; }
    public string Region { get; set; }
    public string CustomerMagic { get; set; }

    public string ItemType { get; set; }
}