using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// Which source of evidence decided that a Special Service Campaign (SSC) / safety recall is repaired.
/// The sources are consulted in the order they are declared here, and the first one that holds wins. A deployment
/// that sets <c>LookupOptions.PreferSSCRepairEvidenceOverRecordDate</c> consults the SSC record last instead.
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
    /// A finished service-history labor line carries one of the campaign's labor operation codes (or a code
    /// configured as interchangeable with one). By default a line is finished when its invoice status is X or C;
    /// a deployment can set its own rule (<c>LookupOptions.SSCQualifyingLaborLine</c>).
    /// </summary>
    ServiceHistory = 3,
}
