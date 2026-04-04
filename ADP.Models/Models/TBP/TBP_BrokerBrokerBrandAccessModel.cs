using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker's access permissions for a specific brand.
/// Controls whether the broker is active for the brand, the account period, and which companies/branches provide service.
/// </summary>
[Docable]
public class TBP_BrokerBrokerBrandAccessModel
{
    /// <summary>
    /// The brand ID this access record applies to.
    /// </summary>
    public long BrandID { get; set; }

    /// <summary>
    /// Whether the broker's access to this brand is currently active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// The date from which the broker's account for this brand became active.
    /// </summary>
    public DateTime? AccountStartDate { get; set; }

    /// <summary>
    /// The date the broker's access to this brand was terminated. Null if still active.
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// The location code associated with this brand access.
    /// </summary>
    public string? LocationCode { get; set; }

    /// <summary>
    /// The company ID associated with this brand access.
    /// </summary>
    public long? CompanyID { get; set; }

    /// <summary>
    /// The company branch ID associated with this brand access.
    /// </summary>
    public long? CompanyBranchID { get; set; }

    /// <summary>
    /// The list of company IDs that provide service for this broker's vehicles under this brand.
    /// </summary>
    public List<long> ServiceCompanies { get; set; } = new();
}
