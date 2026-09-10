using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// Which source of evidence decided that a Special Service Campaign (SSC) / safety recall is repaired.
/// The sources are consulted in the order they are declared here, and the first one that holds wins.
/// </summary>
[Docable]
public enum SscRepairSource
{
    /// <summary>No repair evidence was found; the campaign is still open on this vehicle.</summary>
    None = 0,
    /// <summary>The SSC record itself carries a repair date.</summary>
    SSCRecord = 1,
    /// <summary>
    /// A warranty claim in a qualifying status (Accepted, Certified or Invoiced) references the campaign — either
    /// by naming the campaign code in the distributor comment, or by carrying one of the campaign's labor
    /// operation codes (or a code configured as interchangeable with one).
    /// </summary>
    WarrantyClaim = 2,
    /// <summary>
    /// An invoiced service-history labor line (invoice status X or C) carries one of the campaign's labor
    /// operation codes (or a code configured as interchangeable with one).
    /// </summary>
    ServiceHistory = 3,
}
