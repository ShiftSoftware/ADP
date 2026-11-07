using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleVinDecodeEvaluator
{
    private const int VinLength = 17;
    private const int WmiLength = 3;
    private const int VdsLength = 5;

    private readonly IVehicleLookupStorageService storage;

    public VehicleVinDecodeEvaluator(IVehicleLookupStorageService storage)
    {
        this.storage = storage;
    }

    public async Task<VehicleVinDecodeDTO> Evaluate(string vin)
    {
        vin = vin?.Trim().ToUpperInvariant();

        if (vin?.Length != VinLength || !vin.All(char.IsLetterOrDigit))
            return null;

        var mapping = await storage.GetVinModelMappingAsync(vin.Substring(0, WmiLength), vin.Substring(WmiLength, VdsLength));

        var katashiki = Clean(mapping?.Katashiki);
        var modelDescription = Clean(mapping?.ModelDescription);

        if (katashiki is null && modelDescription is null)
            return null;

        return new VehicleVinDecodeDTO
        {
            Katashiki = katashiki,
            ModelDescription = modelDescription,
        };
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
