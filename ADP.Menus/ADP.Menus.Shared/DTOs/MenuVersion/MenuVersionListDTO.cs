using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;

[ShiftEntityKeyAndName(nameof(VersionDateTime), nameof(Text))]
public class MenuVersionListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public int Version { get; set; }
    public DateTimeOffset VersionDateTime { get; set; }

    public string Text { get; set; }
}
