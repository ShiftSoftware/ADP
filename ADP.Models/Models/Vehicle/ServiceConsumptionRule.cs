using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Opts an offer into lookup-only consumption from strongly consistent service invoices.
/// This does not create a claim or record a financial cost.
/// </summary>
public class ServiceConsumptionRule
{
    /// <summary>
    /// Null matches any service invoice. Otherwise at least one labor package code must match
    /// exactly, ignoring case. Empty lists or blank codes fail closed.
    /// </summary>
    public IEnumerable<string> PackageCodes { get; set; }
}
