namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class ReplacementItemDefaultPartDTO
{
    public string PartNumber { get => field; set => field = value?.Trim() ?? string.Empty; }

    public decimal? DefaultPeriodicQuantity { get; set; }

    public decimal? DefaultStandaloneQuantity { get; set; }
}
