using ShiftSoftware.ADP.Models;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

/// <summary>
/// The result status of an SSC (Special Service Campaign) / safety recall lookup.
/// </summary>
[Docable]
public enum SSCLookupStatuses
{
    /// <summary>No active recalls found for this vehicle.</summary>
    [Description("No Recall")]
    NoRecall = 0,

    /// <summary>One or more active recalls exist for this vehicle.</summary>
    [Description("Recall Exists")]
    RecallExists = 1,

    /// <summary>The vehicle was not found in the manufacturer's recall database.</summary>
    [Description("Vehicle not Found")]
    NoApplicableVehicleFound = 2,

    /// <summary>An error occurred while communicating with the manufacturer's (TMC) recall system.</summary>
    [Description("TMC Error")]
    TmcErrorResponse = 3,

    /// <summary>No response was received from the relay server.</summary>
    [Description("No Response from Relay Server")]
    RelayServerNoResponse = 4,

    /// <summary>The VIN is unauthorized and a TMC lookup has been queued. Results will be available on the next lookup.</summary>
    [Description("Pending TMC Lookup")]
    PendingTMCLookup = 100,
}