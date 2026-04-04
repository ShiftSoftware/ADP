namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The status of a part lookup request sent to the manufacturer's system.
/// </summary>
[Docable]
public enum ManufacturerPartLookupStatus
{
    /// <summary>
    /// The lookup request is pending a response from the manufacturer.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The manufacturer returned a result for the lookup request.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// The manufacturer could not resolve the lookup request.
    /// </summary>
    UnResolved = 2,
}
