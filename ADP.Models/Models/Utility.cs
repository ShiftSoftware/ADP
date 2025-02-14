using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

namespace ShiftSoftware.ADP.Models;

public class Utility
{
    public static VehicleVariantInfoDTO ParseVariantInfo(string variant)
    {
        try
        {
            var modelCode = variant.Substring(0, variant.Length - 8);
            var sfx = variant.Substring(variant.Length - 8, 2);
            var yearModel = variant.Substring(variant.Length - 6, 4);

            return new VehicleVariantInfoDTO
            {
                ModelCode = modelCode,
                SFX = sfx,
                ModelYear = int.Parse(yearModel)
            };
        }
        catch
        {
            return null;
        }
    }
}
