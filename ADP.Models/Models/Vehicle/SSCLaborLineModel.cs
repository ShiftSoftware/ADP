namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a single labor operation required for a Special Service Campaign (SSC) / safety recall repair.
/// </summary>
[Docable]
public class SSCLaborLineModel
{
    /// <summary>
    /// The labor operation code required for the recall repair.
    /// </summary>
    public string LaborCode { get; set; }

    /// <summary>
    /// The estimated hours for this labor operation.
    /// </summary>
    public double? LaborHour { get; set; }
}
