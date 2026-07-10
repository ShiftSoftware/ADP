using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;

public class WarrantyCertificateLineDTO
{
    public ShiftEntitySelectDTO? WarrantyClaim { get; set; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? DistributorProcessDate { get; set; }
    public string? WarrantyType { get; set; }
}
