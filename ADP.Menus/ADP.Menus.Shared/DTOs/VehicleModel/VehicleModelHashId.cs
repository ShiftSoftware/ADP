using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class VehicleModelHashId : JsonHashIdConverterAttribute<VehicleModelHashId>
{
    public VehicleModelHashId() : base(5)
    {
        
    }
}
