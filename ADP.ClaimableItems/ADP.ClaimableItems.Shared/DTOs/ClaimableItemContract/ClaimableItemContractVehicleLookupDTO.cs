using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

/// <summary>
/// The answer to the contract form's VIN lookup: the vehicle, the country the menu is priced for, and the
/// contract-type menu lines that can be put on a contract.
/// </summary>
public class ClaimableItemContractVehicleLookupDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractLookupStatus Status { get; set; }

    public string VIN { get; set; } = default!;
    public string? Katashiki { get; set; }
    public string? BasicModelCode { get; set; }
    public string? Variant { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelDescription { get; set; }

    [BrandHashIdConverter]
    public ShiftEntitySelectDTO? Brand { get; set; }

    /// <summary>The countries a contract can be priced for. Empty when none is configured.</summary>
    [CountryHashIdConverter]
    public List<ShiftEntitySelectDTO> Countries { get; set; } = new();

    /// <summary>
    /// True when the server chose the country and the transfer rate (one country or none is configured),
    /// so the form shows neither field.
    /// </summary>
    public bool CountryIsFixed { get; set; }

    [CountryHashIdConverter]
    public ShiftEntitySelectDTO? Country { get; set; }

    public decimal TransferRate { get; set; }

    public string MenuLanguage { get; set; } = default!;

    /// <summary>The price increments a contract may use, as fractions (0.05 is 5%). Host configuration.</summary>
    public List<decimal> PriceIncrements { get; set; } = new();

    /// <summary>The fewest services a contract can be issued with. Host configuration.</summary>
    public int MinimumLines { get; set; }

    /// <summary>
    /// The lines a contract may take. Lines with unpriced parts and periodic lines at or below the mileage
    /// are left out; a periodic line already on another contract is listed with its reason.
    /// </summary>
    public List<ClaimableItemContractMenuLineDTO> Lines { get; set; } = new();
}
