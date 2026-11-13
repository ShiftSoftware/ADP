using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

namespace ShiftSoftware.ADP.ClaimableItems.Shared;

/// <summary>
/// Where service contracts read a vehicle and its contract-type menu from. The host implements it over its own
/// vehicle lookup (an endpoint, usually) and registers it; the module does not read the lookup itself. The
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

    /// <summary>
    /// EVERY contract-type line of one model's menu, by its basic model code: what a contract uses when the
    /// user picks the model because the VIN is not official or has no menu. Only <see cref="ClaimableItemContractVehicleMenu.MenuStatus"/>
    /// and <see cref="ClaimableItemContractVehicleMenu.Lines"/> are read. A host that does not implement it
    /// cannot sell contracts for such vehicles.
    /// </summary>
    Task<ClaimableItemContractVehicleMenu> GetModelMenuAsync(string basicModelCode, long? countryId, decimal transferRate, string language) =>
        Task.FromResult(new ClaimableItemContractVehicleMenu { MenuStatus = ClaimableItemContractVehicleMenuStatus.Unavailable });
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
