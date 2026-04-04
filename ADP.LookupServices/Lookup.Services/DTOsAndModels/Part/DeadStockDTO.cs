using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents dead stock information for a part at a specific company, grouped by branch.
/// </summary>
[TypeScriptModel]
[Docable]
public class DeadStockDTO
{
    /// <summary>The Company Hash ID from the Identity System.</summary>
    public string CompanyHashID { get; set; }
    /// <summary>The resolved company name.</summary>
    public string CompanyName { get; set; }
    /// <summary>Dead stock quantities broken down by branch.</summary>
    public IEnumerable<BranchDeadStockDTO> BranchDeadStock { get; set; } = [];
}