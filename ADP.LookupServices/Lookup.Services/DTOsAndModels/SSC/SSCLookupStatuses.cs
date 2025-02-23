using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public enum SSCLookupStatuses
{
    [Description("No Recall")]
    NoRecall = 0,

    [Description("Recall Exists")]
    RecallExists = 1,

    [Description("TMC Error")]
    TmcErrorResponse = 3,

    [Description("No Response from Relay Server")]
    RelayServerNoResponse = 4,

    /// <summary>
    /// After the first lookup of an unauthorized vin
    /// </summary>
    [Description("Pending TMC Lookup")]
    PendingTMCLookup = 100,
}
