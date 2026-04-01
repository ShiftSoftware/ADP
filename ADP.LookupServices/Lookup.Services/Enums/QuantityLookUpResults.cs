using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The result of a part stock quantity lookup at a specific location.
/// </summary>
[Docable]
public enum QuantityLookUpResults
{
    /// <summary>Stock lookup was skipped (not configured or not applicable).</summary>
    LookupIsSkipped = 0,
    /// <summary>The requested quantity is fully available.</summary>
    Available = 1,
    /// <summary>Some quantity is available but less than requested.</summary>
    PartiallyAvailable = 2,
    /// <summary>The part is not available at this location.</summary>
    NotAvailable = 3,
    /// <summary>The lookup quantity is below the configured threshold for display.</summary>
    QuantityNotWithinLookupThreshold = 4,
}