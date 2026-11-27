using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Cases.Shared.DTOs;

/// <summary>What a list's action button sends: the ticked rows, and the comment each of their history lines gets.</summary>
/// <typeparam name="TListDTO">The case type's list row.</typeparam>
public class CaseBatchActionDTO<TListDTO>
{
    public SelectStateDTO<TListDTO> Selection { get; set; } = new();

    /// <summary>The comment for every history line the action writes. Optional.</summary>
    public string? Comment { get; set; }
}
