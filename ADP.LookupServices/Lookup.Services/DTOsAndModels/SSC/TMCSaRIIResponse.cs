using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

/// <summary>
/// Response from the TMC SaRII (Safety Recall Information Interface) system for SSC lookups.
/// </summary>
[Docable]
public class TMCSaRIIResponse
{
    /// <summary>The result status of the SSC lookup.</summary>
    public SSCLookupStatuses SSCLookupStatus { get; set; }
    /// <summary>The TMC error code if an error occurred (0 if no error).</summary>
    public int TMCErrorCode { get; set; }
    /// <summary>The exception message if an error occurred.</summary>
    public string ExceptionMessage { get; set; }
}