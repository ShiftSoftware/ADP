using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;

public class ServiceIntervalGroupHashId : JsonHashIdConverterAttribute<ServiceIntervalGroupHashId>
{
    public ServiceIntervalGroupHashId() : base(5)
    {
    }
}
