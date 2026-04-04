using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// Represents a part lookup request sent to the manufacturer's system.
/// Tracks who requested it, the part details, order type, and the manufacturer's response.
/// </summary>
[Docable]
public class ManufacturerPartLookupModel :
    ICompanyProps,
    IBranchProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// The ID of the user who initiated the lookup request.
    /// </summary>
    public long? UserID { get; set; }

    /// <summary>
    /// The email address of the user who initiated the lookup request.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// The part number being looked up at the manufacturer.
    /// </summary>
    public string PartNumber { get; set; }

    /// <summary>
    /// The quantity requested for this part.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// The type of manufacturer order (e.g., Stock or Emergency).
    /// </summary>
    public ManufacturerOrderType OrderType { get; set; }

    /// <summary>
    /// An optional log identifier for tracing this lookup request.
    /// </summary>
    public string? LogId { get; set; }

    /// <summary>
    /// The current status of this manufacturer part lookup request.
    /// </summary>
    public ManufacturerPartLookupStatus Status { get; set; }

    [DocIgnore]
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Key-value pairs returned by the manufacturer's system as the lookup result.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>>? ManufacturerResult { get; set; } = [];
}
