using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleLookupRequestOptions
{
    public string LanguageCode { get; set; } = "en";
    public bool IgnoreBrokerStock { get; set; }
    public bool InsertSSCLog { get; set; }
    public SSCLogInfo SSCLogInfo { get; set; }
    public bool InsertCustomerVehcileLookupLog { get; set; }
    public CustomerVehicleLookupLogInfo CustomerVehicleLookupLogInfo { get; set; }
}