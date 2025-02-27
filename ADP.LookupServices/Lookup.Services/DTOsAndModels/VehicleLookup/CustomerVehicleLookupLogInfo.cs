using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class CustomerVehicleLookupLogInfo
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public LookupSources? VehicleLookupSource { get; set; }

    /// <summary>
    /// The Brand that the user made the lookup for
    /// </summary>
    public Brands? LookupBrand { get; set; }

    public long? CityID { get; set; }
}