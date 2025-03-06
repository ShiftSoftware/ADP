using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyClaimModel : IPartitionedItem, IBrandProps, ICompanyProps
{
    public string id { get; set; }
    public long ID { get; set; }
    public string VIN { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? RepairDate { get; set; }
    public WarrantyClaimStatus ClaimStatus { get; set; }
    public WarrantyManufacturerClaimStatus ManufacturerStatus { get; set; }
    public string DistributorComment { get; set; }
    public string LaborOperationNoMain { get; set; }
    public string ItemType => ModelTypes.WarrantyClaim;
    public string ClaimNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public string DealerClaimNumber { get; set; }
    public DateTime? DateOfReceipt { get; set; }
    public string WarrantyType { get; set; }
    public string BrandIntegrationID { get; set; }
    public Brands Brand { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }
    public string RepairOrderNo { get; set; } = default!;
    public DateTime? ProcessDate { get; set; }
    public DateTime? DistributorProcessDate { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyIntegrationID { get; set; }
    public IEnumerable<WarrantyClaimLaborLineModel> LaborLines { get; set; }
}