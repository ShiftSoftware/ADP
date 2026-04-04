namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The shipping method for manufacturer part orders.
/// </summary>
[Docable]
public enum ManufacturerOrderType
{
    /// <summary>
    /// Standard shipping by sea freight.
    /// </summary>
    Sea = 0,

    /// <summary>
    /// Express shipping by air freight.
    /// </summary>
    Airplane = 1,
}
