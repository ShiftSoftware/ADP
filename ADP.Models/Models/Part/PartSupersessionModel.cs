namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Represents a part's supersession information. A part can be superseded by another part.
/// </summary>
[Docable]
public class PartSupersessionModel
{
    /// <summary>
    /// The part number of the superseding part.
    /// </summary>
    public string PartNumber { get; set; } = default!;


    public int? SupersessionCode { get; set; } = default!;
}