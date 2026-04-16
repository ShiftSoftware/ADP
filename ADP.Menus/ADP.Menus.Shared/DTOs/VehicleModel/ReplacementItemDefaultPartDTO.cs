namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class ReplacementItemDefaultPartDTO
{
    public long? ID { get; set; }

    public string PartNumber { get => field; set => field = value?.Trim() ?? string.Empty; }

    public decimal? DefaultPeriodicQuantity { get; set; }

    public decimal? DefaultStandaloneQuantity { get; set; }
}
