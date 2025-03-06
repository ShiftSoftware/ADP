namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyClaimLaborLineModel
{
    public long ID { get; set; }
    public string PayCode { get; set; }
    public bool MainOperation { get; set; }
    public string LaborCode { get; set; }
    public decimal Hour { get; set; }
    public decimal? DistributorHour { get; set; }
}