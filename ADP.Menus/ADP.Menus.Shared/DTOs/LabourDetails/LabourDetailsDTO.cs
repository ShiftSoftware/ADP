using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;

public class LabourDetailsDTO
{
    [Required]
    [ServiceIntervalGroupHashId]
    public string ServiceIntervalGroupID { get; set; } = default!;

    public string? Name { get; set; }

    [Required]
    public decimal? AllowedTime { get; set; }

    [Required]
    public decimal? Consumable { get; set; }
}
