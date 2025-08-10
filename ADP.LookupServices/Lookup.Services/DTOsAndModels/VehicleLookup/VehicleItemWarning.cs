using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup
{
    [TypeScriptModel]
    public class VehicleItemWarning
    {
        public string Title { get; set; }
        public string BodyContent { get; set; }
        public string? ImageUrl { get; set; }
        public string? ConfiramtionText { get; set; }
    }
}
