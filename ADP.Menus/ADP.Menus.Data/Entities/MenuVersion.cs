using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

public class MenuVersion : ShiftEntity<MenuVersion>
{
    public int Version { get; set; }
    public DateTimeOffset VersionDateTime { get; set; }
}
