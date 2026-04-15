namespace ShiftSoftware.ADP.Menus.Data.DataServices;

public class RTSCodeExportModel
{
    public string LabourCode { get; set; } = default!;
    public decimal LabourRate { get; set; }
    public decimal AllowedTime { get; set; }
    public string Description { get; set; } = default!;
    public long? BrandID { get; set; }
}
