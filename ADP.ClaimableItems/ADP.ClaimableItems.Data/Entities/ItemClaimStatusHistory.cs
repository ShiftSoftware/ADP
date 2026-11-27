using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Entities;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

[Index(nameof(ClaimID), nameof(Timestamp))]
public class ItemClaimStatusHistory : ClaimStatusHistoryBase<ItemClaimStatusHistory, ItemClaim>
{
}
