using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(ReplacementItemID), nameof(ServiceIntervalGroupID), IsUnique = true)]
[TemporalShiftEntity]
public class ReplacementItemServiceIntervalGroup : ShiftEntity<ReplacementItemServiceIntervalGroup>
{
    public long ReplacementItemID { get; set; }
    public ReplacementItem ReplacementItem { get; set; }

    public long ServiceIntervalGroupID { get; set; }
    public ServiceIntervalGroup ServiceIntervalGroup { get; set; }

    public ReplacementItemServiceIntervalGroup()
    {
        
    }

    public ReplacementItemServiceIntervalGroup(long id) : base(id)
    {
        
    }
}
