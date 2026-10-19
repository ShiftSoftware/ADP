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

    /// <summary>The description a scheduled line of a <c>Paid</c> variant carries.</summary>
    public string? PaidDescription { get; set; }

    /// <summary>The description a scheduled line of a <c>Free</c> variant carries.</summary>
    public string? FreeDescription { get; set; }

    /// <summary>The description a scheduled line of a <c>Contract</c> variant carries.</summary>
    public string? ContractDescription { get; set; }

    [ServiceIntervalGroupHashId]
    [Required]
    public ShiftEntitySelectDTO ServiceIntervalGroup { get; set; } = default!;
}
