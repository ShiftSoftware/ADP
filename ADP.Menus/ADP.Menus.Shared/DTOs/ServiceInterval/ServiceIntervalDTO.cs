using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;

public class ServiceIntervalDTO : ShiftEntityViewAndUpsertDTO
{
    [ServiceIntervalHashId]
    public override string? ID { get; set; }
    public string Code { get; set; }
    public string FullName { get; set; }

    [Required]
    public int? ValueInMeter { get; set; }
    public string? Description { get; set; }

    [ServiceIntervalGroupHashId]
    [Required]
    public ShiftEntitySelectDTO ServiceIntervalGroup { get; set; } = default!;
}
