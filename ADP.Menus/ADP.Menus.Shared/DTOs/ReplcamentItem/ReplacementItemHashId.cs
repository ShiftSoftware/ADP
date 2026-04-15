using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;

public class ReplacementItemHashId : JsonHashIdConverterAttribute<ReplacementItemHashId>
{
    public ReplacementItemHashId() : base(5)
    {

    }
}
