using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class FreeServiceItemDateShiftModel : IPartitionedItem, ICompanyProps
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime NewDate { get; set; }
    public string ItemType => ModelTypes.FreeServiceItemDateShift;
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public bool IsDeleted { get; set; }
}