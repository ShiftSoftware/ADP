using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker entity in the Third-Party Broker (TBP) system.
/// Contains the broker's identity, region, account numbers, and brand access permissions.
/// </summary>
[Docable]
public class TBP_BrokerModel : IRegionProps
{
    /// <summary>
    /// The unique identifier for this broker.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The broker's name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Indicates whether this broker has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The <see cref="TBP_BrokerAccountNumberModel">account numbers</see> associated with this broker across different companies.
    /// </summary>
    public IEnumerable<TBP_BrokerAccountNumberModel> AccountNumbers { get; set; }

    [DocIgnore]
    public long? RegionID { get; set; }

    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    public string RegionHashID { get; set; }

    /// <summary>
    /// The <see cref="TBP_BrokerBrokerBrandAccessModel">brand access</see> permissions for this broker.
    /// </summary>
    public IEnumerable<TBP_BrokerBrokerBrandAccessModel> BrandAccesses { get; set; }
}
