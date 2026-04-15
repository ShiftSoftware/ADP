using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;

public class LabourRateByCountryDTO
{
    [Required]
    public long? CountryID { get; set; }

    [Required]
    public decimal? LabourRate { get; set; }
}
