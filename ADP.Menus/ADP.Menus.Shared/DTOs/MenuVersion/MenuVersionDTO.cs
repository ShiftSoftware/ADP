using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;

public class MenuVersionDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    /// <summary>
    /// No need to provide value in request, it will be generated automatically. and it needed for response.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// No need to provide value in request, it will be generated automatically. and it needed for response.
    /// </summary>
    public DateTimeOffset VersionDateTime { get; set; }
}
