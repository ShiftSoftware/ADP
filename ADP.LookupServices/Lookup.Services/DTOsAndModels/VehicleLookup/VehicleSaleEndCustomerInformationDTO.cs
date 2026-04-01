using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains the end customer's information for a vehicle sale.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleSaleEndCustomerInformationDTO
{
    /// <summary>The customer's unique identifier.</summary>
    public string ID { get; set; }
    /// <summary>The customer's full name.</summary>
    public string Name { get; set; }
    /// <summary>The customer's phone number.</summary>
    public string Phone { get; set; }
    /// <summary>The customer's government-issued ID number.</summary>
    public string IDNumber { get; set; }
}