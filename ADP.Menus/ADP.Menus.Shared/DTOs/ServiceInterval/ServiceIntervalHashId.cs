using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;

public class ServiceIntervalHashId : JsonHashIdConverterAttribute<ServiceIntervalHashId>
{
    public ServiceIntervalHashId() : base(5)
    {
    }
}
