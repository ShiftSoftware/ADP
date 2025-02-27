using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;

public enum LookupSources
{
    [Description("Dealers Portal")]
    DealersPortal = 1,

    [Description("Website")]
    Website = 3,

    [Description("App")]
    App = 4,

    [Description("Hub")]
    Hub = 5,

    [Description("Info Web App")]
    InfoWebApp = 6,
}