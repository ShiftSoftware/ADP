
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[TemporalShiftEntity]
public class ManufacturerSettlmentSheet : ShiftEntity<ManufacturerSettlmentSheet>
{
    public bool PreventSubmittingUnrecognizedClaimNumbers { get; set; }
    public string? InvoiceNumbers { get; set; }
    public string? Attachments { get; set; }
    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new HashSet<WarrantyClaim>();
    public decimal ExchangeRate { get; set; }
}
