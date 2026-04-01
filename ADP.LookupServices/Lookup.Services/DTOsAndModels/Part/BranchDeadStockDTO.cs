using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents dead stock quantity for a part at a specific branch.
/// </summary>
[TypeScriptModel]
[Docable]
public class BranchDeadStockDTO
{
    /// <summary>The Branch Hash ID from the Identity System.</summary>
    public string CompanyBranchHashID { get; set; }
    /// <summary>The resolved branch name.</summary>
    public string CompanyBranchName { get; set; }
    /// <summary>The dead stock quantity at this branch.</summary>
    public decimal Quantity { get; set; }
}