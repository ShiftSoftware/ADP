using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.ManufacturerSettlmentSheet;

public class ManufacturerSettlmentSheetDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string? InvoiceNumbers { get; set; }
    public bool PreventSubmittingUnrecognizedClaimNumbers { get; set; } = true;

    [MinLength(1, ErrorMessage = "At least one Attachment is required")]
    [Required(ErrorMessage = "At least one Attachment is required")]
    public List<ShiftFileDTO>? Attachments { get; set; }

    [Range(80, 250, ErrorMessage = "Invalid Value")]
    [Required(ErrorMessage = "Required")]
    public decimal ExchangeRate { get; set; }
}
