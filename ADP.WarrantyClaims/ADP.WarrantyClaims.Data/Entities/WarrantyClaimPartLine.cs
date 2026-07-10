using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[TemporalShiftEntity]
public class WarrantyClaimPartLine : ShiftEntity<WarrantyClaimPartLine>
{
    public string PayCode { get; set; } = default!;
    public bool OFP { get; set; }
    public string LocalF { get; set; } = default!;
    public string PartNumber { get; set; } = default!;
    public string? PartDescription { get; set; }
    public int? Qty { get; set; }
    public decimal Price { get; set; }
    public decimal? DistributorPrice { get; set; }
    public decimal? DistributorPriceJPY { get; set; }
    public bool FoundInLookup { get; set; }

    public long WarrantyClaimID { get; set; }
    public virtual WarrantyClaim WarrantyClaim { get; set; } = default!;
}
