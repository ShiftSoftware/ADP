using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class VehicleModelMenuDTO
{
    [Required]
    public decimal? LabourRate { get; set; }

    [Required]
    public IEnumerable<LabourRateByCountryDTO> LabourRates { get; set; } = [];

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; }

    public string VehicleModelName { get; set; } = default!;

    [VehicleModelHashId]
    public string VehicleModelID { get; set; }

    [Required]
    public IEnumerable<LabourDetailsDTO> LabourDetails { get; set; }


    [Required]
    public IEnumerable<MenuReplacementItemDTO> ReplacementItems { get; set; }
}
