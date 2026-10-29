using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

namespace ShiftSoftware.ADP.ClaimableItems.Shared;

/// <summary>
/// Where service contracts read a vehicle and its contract-type menu from. The API package registers a
/// default that goes through the vehicle lookup (so the host must call <c>AddLookupService</c>) and reads the
/// countries from the host's <c>IMenuCountryProvider</c>. A host can register its own instead; the
/// module's rules (mileage, unpriced parts, other contracts) are applied on top of whatever this returns.
/// </summary>
public interface IClaimableItemContractMenuProvider
{
    /// <summary>The countries a contract can be priced for. Empty when the host configures none.</summary>
    ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync();

    /// <summary>
    /// The vehicle and EVERY contract-type line on its menu, unfiltered.
    /// </summary>
    /// <param name="countryId">Null when no country is configured: the lookup's default applies.</param>
    Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language);
}

public class ClaimableItemContractCountry
{
    public long ID { get; set; }
    public string Name { get; set; } = default!;
}

public class ClaimableItemContractVehicleMenu
{
    /// <summary>The vehicle is one of the distributor's official vehicles.</summary>
    public bool IsOfficial { get; set; }

    public string? Katashiki { get; set; }
    public string? BasicModelCode { get; set; }
    public string? Variant { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelDescription { get; set; }
    public long? BrandID { get; set; }

    public ClaimableItemContractVehicleMenuStatus MenuStatus { get; set; }

    /// <summary>Every contract-type line, with <see cref="ClaimableItemContractMenuLineDTO.UnavailableReason"/> unset.</summary>
    public List<ClaimableItemContractMenuLineDTO> Lines { get; set; } = new();
}

public enum ClaimableItemContractVehicleMenuStatus
{
    Found = 0,
    NotFound = 1,
    Unavailable = 2,
}
