using ShiftSoftware.ADP.ClaimableItems.Shared;

namespace ShiftSoftware.ADP.ClaimableItems.API.Services;

/// <summary>
/// Registered until the host registers its own <see cref="IClaimableItemContractMenuProvider"/>, so a host that
/// never opens a contract still starts, and one that does gets told what is missing.
/// </summary>
internal class UnconfiguredContractMenuProvider : IClaimableItemContractMenuProvider
{
    public ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync() => throw NotConfigured();

    public Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language) =>
        throw NotConfigured();

    public Task<ClaimableItemContractVehicleMenu> GetModelMenuAsync(string basicModelCode, long? countryId, decimal transferRate, string language) =>
        throw NotConfigured();

    private static InvalidOperationException NotConfigured() => new(
        "Service contracts need the host to register an IClaimableItemContractMenuProvider that returns the vehicle " +
        "and its contract-type menu from the host's vehicle lookup.");
}
