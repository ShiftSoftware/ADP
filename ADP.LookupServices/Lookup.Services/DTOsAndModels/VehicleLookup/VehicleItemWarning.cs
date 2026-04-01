using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup
{
    /// <summary>
    /// Represents a warning message displayed to the user before claiming a service item.
    /// </summary>
    [TypeScriptModel]
    [Docable]
    public class VehicleItemWarning
    {
        /// <summary>A unique key identifying this warning type.</summary>
        public string Key { get; set; }
        /// <summary>The warning title.</summary>
        public string Title { get; set; }
        /// <summary>An optional image URL displayed with the warning.</summary>
        public string? ImageUrl { get; set; }
        /// <summary>The main body content/message of the warning.</summary>
        public string BodyContent { get; set; }
        /// <summary>Optional confirmation text the user must acknowledge to proceed.</summary>
        public string? ConfirmationText{ get; set; }
    }
}
