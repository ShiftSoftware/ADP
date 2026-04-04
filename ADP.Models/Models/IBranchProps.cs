namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines branch-level properties for models that belong to a specific company branch.
/// Extends <see cref="ICompanyProps"/> with branch identification.
/// </summary>
internal interface IBranchProps : ICompanyProps
{
    /// <summary>
    /// The internal branch ID.
    /// </summary>
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }
}