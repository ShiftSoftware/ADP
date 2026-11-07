namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Maps a VIN's first 8 characters, the WMI and the VDS, to the model they point to.
/// Each host loads its own rows, and these packages ship none.
/// </summary>
[Docable]
public class VinModelMappingModel
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The World Manufacturer Identifier: VIN characters 1 to 3.
    /// </summary>
    public string WMI { get; set; }

    /// <summary>
    /// The Vehicle Descriptor Section: VIN characters 4 to 8.
    /// </summary>
    public string VDS { get; set; }

    /// <summary>
    /// The base Katashiki these characters point to: the code before the hyphen. Empty when they do not settle on one code.
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The model name these characters point to.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// Where the host's row came from. Not shown to anyone; it is there for whoever maintains the rows.
    /// </summary>
    public string Source { get; set; }
}
