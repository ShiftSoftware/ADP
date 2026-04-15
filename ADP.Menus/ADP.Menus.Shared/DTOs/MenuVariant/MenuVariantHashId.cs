using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;

public class MenuVariantHashId : JsonHashIdConverterAttribute<MenuVariantHashId>
{
    public MenuVariantHashId() : base(35)
    {
    }
}
