namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam for <c>DeliveryDateService</c>'s last-service-activation fallback: when no warranty
/// claim carries a delivery date for a VIN, the suggested date falls back to the vehicle's last service
/// activation date. ServiceActivation is a consumer-owned entity, so the module takes this optional
/// provider instead of depending on it. A distributor consumer can implement it over its
/// <c>ServiceActivationRepository</c> (reads <c>ServiceActivation.WarrantyActivationDate</c>). When no
/// provider is registered the fallback simply yields no suggestion.
/// </summary>
public interface ILastServiceActivationDateProvider
{
    Task<DateTime?> GetLastServiceActivationAsync(string vin);
}
