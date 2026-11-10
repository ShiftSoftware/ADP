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
    private readonly LookupOptions options;

    /// <param name="options">Its clock places the model year's 30-year window. Without it, the system clock does.</param>
    public VehicleVinDecodeEvaluator(IVehicleLookupStorageService storage, LookupOptions options = null)
    {
        this.storage = storage;
        this.options = options ?? new LookupOptions();
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
            // Only for a VIN with a mapping row: the host's rows name the manufacturers it decodes, and those are
            // the ones known to put the year code in character 10. Other manufacturers may use it for something else.
            ModelYear = VinModelYear.Decode(vin, options.GetUtcNow().Year),
        };
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
