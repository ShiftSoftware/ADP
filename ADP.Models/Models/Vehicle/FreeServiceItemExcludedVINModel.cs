namespace ShiftSoftware.ADP.Models.Vehicle;

public class FreeServiceItemExcludedVINModel : IPartitionedItem, ICompanyProps
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => ModelTypes.FreeServiceItemExcludedVIN;
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public bool IsDeleted { get; set; }
}