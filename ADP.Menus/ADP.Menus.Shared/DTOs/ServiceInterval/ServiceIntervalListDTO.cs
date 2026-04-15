using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;

public class ServiceIntervalListDTO : ShiftEntityListDTO
{
    [ServiceIntervalHashId]
    public override string? ID { get; set; }
    public string Code { get; set; }
    public string FullName { get; set; }
    public int ValueInMeter { get; set; }
    public string? Description { get; set; }
    public string ServiceIntervalGroupName { get; set; }
}
