using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[TemporalShiftEntity]
public class WarrantyClaimLaborLine : ShiftEntity<WarrantyClaimLaborLine>
{
    public string PayCode { get; set; } = default!;
    public bool MainOperation { get; set; }
    public string OperationNumber { get; set; } = default!;

    [Precision(18, 1)]
    public decimal Hour { get; set; }

    [Precision(18, 1)]
    public decimal? DistributorHour { get; set; }

    public long WarrantyClaimID { get; set; }
    public virtual WarrantyClaim WarrantyClaim { get; set; } = default!;
}
