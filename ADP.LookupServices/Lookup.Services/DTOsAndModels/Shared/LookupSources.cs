using ShiftSoftware.ADP.Models;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;

/// <summary>
/// The source/channel from which a lookup request originated.
/// </summary>
[Docable]
public enum LookupSources
{
    /// <summary>Lookup from the dealers portal web application.</summary>
    [Description("Dealers Portal")]
    DealersPortal = 1,
    /// <summary>Lookup from the public website.</summary>
    [Description("Website")]
    Website = 3,
    /// <summary>Lookup from the mobile app.</summary>
    [Description("App")]
    App = 4,
    /// <summary>Lookup from the hub application.</summary>
    [Description("Hub")]
    Hub = 5,
    /// <summary>Lookup from the info web application.</summary>
    [Description("Info Web App")]
    InfoWebApp = 6,
}