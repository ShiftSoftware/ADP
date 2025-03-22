namespace ShiftSoftware.ADP.Models.TBP;

public class BrokerVehicleModel
{
    public string VIN { get; set; } = default!;
    public string Model { get; set; }
    public string ModelYear { get; set; }
    public string Engine { get; set; }
    public string VariantDescription { get; set; }
    public string VariantCode { get; set; }
    public string Katashiki { get; set; }
    public string Cylinders { get; set; }
    public long? StockRegionID { get; set; }
    public VehicleColorModel ExteriorColor { get; set; } = new();
    public VehicleColorModel InteriorColor { get; set; } = new();
}