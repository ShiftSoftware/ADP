using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.ManufacturerSettlmentSheet;
public class ManufacturerSettlmentSheetListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string? InvoiceNumbers { get; set; }
}
