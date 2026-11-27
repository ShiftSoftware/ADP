using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[Index(nameof(ClaimID), nameof(Timestamp))]
public class WarrantyClaimStatusHistory : ClaimStatusHistoryBase<WarrantyClaimStatusHistory, WarrantyClaim>
{
}
