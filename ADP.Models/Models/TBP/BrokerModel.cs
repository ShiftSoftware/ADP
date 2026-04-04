using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker entity linked to a company in the Identity System.
/// Contains the broker's account period, account numbers, and deletion status.
/// </summary>
[Docable]
public class BrokerModel : ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The unique identifier for this broker.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The broker's name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The date from which the broker's account became active.
    /// </summary>
    public DateTime? AccountStartDate { get; set; }

    /// <summary>
    /// The date the broker's account was terminated. Null if still active.
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// The list of account numbers assigned to this broker.
    /// </summary>
    public IEnumerable<string> AccountNumbers { get; set; }

    /// <summary>
    /// Indicates whether this broker has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}