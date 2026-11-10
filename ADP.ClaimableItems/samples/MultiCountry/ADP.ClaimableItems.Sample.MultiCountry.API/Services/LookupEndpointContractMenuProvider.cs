using ShiftSoftware.ADP.ClaimableItems.Shared;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Services;

/// <summary>
/// To read the vehicle and its contract menu from the distributor's vehicle lookup endpoint, priced for the country
/// and transfer rate the user picked. Not implemented yet.
/// </summary>
public class LookupEndpointContractMenuProvider : IClaimableItemContractMenuProvider
{
    public ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync() =>
        throw new NotImplementedException();

    public Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language) =>
        throw new NotImplementedException();
}
