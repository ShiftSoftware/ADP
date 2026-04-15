using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class ReplacementItem : ShiftEntity<ReplacementItem>
{
    public string Name { get; set; } = default!;
    public string FriendlyName { get; set; } = default!;
    public ReplacementItemType Type { get; set; }
    public bool AllowMultiplePartNumbers { get; set; }

    [Precision(9, 3)]
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    public string StandaloneOperationCode { get; set; } = default!;
    public string StandaloneLabourCode { get; set; } = default!;
    public long? StandaloneReplacementItemGroupID { get; set; }
    public StandaloneReplacementItemGroup? StandaloneReplacementItemGroup { get; set; }

    public virtual ICollection<ReplacementItemServiceIntervalGroup> ReplacementItemServiceIntervalGroups { get; set; } = new HashSet<ReplacementItemServiceIntervalGroup>();
    public virtual ICollection<ReplacementItemVehicleModel> ReplacementItemVehicleModels { get; set; } = new HashSet<ReplacementItemVehicleModel>();

    public ReplacementItem()
    {

    }

    public ReplacementItem(long id) : base(id)
    {

    }
}
