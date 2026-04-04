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


    /// <summary>
    /// The supersession code indicating the type of supersession (e.g., direct replacement, optional alternative).
    /// </summary>
    public int? SupersessionCode { get; set; } = default!;
}