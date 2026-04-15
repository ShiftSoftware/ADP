using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class ServiceIntervalGroup : ShiftEntity<ServiceIntervalGroup>
{
    public string Name { get; set; } = default!;

    public string LabourCode { get; set; } = default!;

    public string LabourDescription { get; set; }

    public ICollection<MenuLabourDetails> MenuLabourDetails { get; set; } = new HashSet<MenuLabourDetails>();
    public ICollection<VehicleModelLabourDetails> VehicleModelLabourDetails { get; set; } = new HashSet<VehicleModelLabourDetails>();
    public IEnumerable<ReplacementItemServiceIntervalGroup>? ReplacementItemServiceIntervalGroups { get; set; }
    public IEnumerable<ServiceInterval> ServiceIntervals { get; set; } = new HashSet<ServiceInterval>();

    public ServiceIntervalGroup()
    {
        
    }

    public ServiceIntervalGroup(long id) : base(id)
    {
        
    }
}

