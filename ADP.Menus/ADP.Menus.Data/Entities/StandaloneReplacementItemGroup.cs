using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class StandaloneReplacementItemGroup : ShiftEntity<StandaloneReplacementItemGroup>
{
    public string Name { get; set; } = default!;
    public string MenuCode { get; set; } = default!;
    public string LabourCode { get; set; } = default!;
    public IEnumerable<ReplacementItem> ReplacementItems { get; set; }
}
