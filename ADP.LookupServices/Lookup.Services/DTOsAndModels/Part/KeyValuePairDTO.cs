using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// A generic key-value pair used for manufacturer lookup results and other dynamic data.
/// </summary>
[TypeScriptModel]
[Docable]
public class KeyValuePairDTO
{
    /// <summary>The key/name.</summary>
    public string Key { get; set; }
    /// <summary>The value.</summary>
    public string Value { get; set; }
}
