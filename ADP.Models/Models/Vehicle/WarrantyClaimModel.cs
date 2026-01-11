using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyClaimModel : IPartitionedItem, IBrandProps, ICompanyProps
{
    public string id { get; set; }
    public string VIN { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? RepairDate { get; set; }
    public ClaimStatus ClaimStatus { get; set; }
    public WarrantyManufacturerClaimStatus ManufacturerStatus { get; set; }
    public string DistributorComment { get; set; }
    public string LaborOperationNumberMain { get; set; }
    public string ItemType => ModelTypes.WarrantyClaim;
    public string ClaimNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public string DealerClaimNumber { get; set; }
    public DateTime? DateOfReceipt { get; set; }
    public string WarrantyType { get; set; }
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }
    public string RepairOrderNumber { get; set; } = default!;
    public DateTime? ProcessDate { get; set; }
    public DateTime? DistributorProcessDate { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public IEnumerable<WarrantyClaimLaborLineModel> LaborLines { get; set; }
}