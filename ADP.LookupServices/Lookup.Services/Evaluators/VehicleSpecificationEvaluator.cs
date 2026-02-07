using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleSpecificationEvaluator
{
    private readonly IVehicleLoockupStorageService VehicleLoockupCosmos;
    public VehicleSpecificationEvaluator(IVehicleLoockupStorageService vehicleLoockupCosmos)
    {
        this.VehicleLoockupCosmos = vehicleLoockupCosmos;
    }

    public async Task<VehicleSpecificationDTO> Evaluate(VehicleEntryModel vehicle)
    {
        VehicleSpecificationDTO result = new();

        var vehicleModel = await VehicleLoockupCosmos.GetVehicleModelsAsync(vehicle?.VariantCode, vehicle?.BrandID);
        var exteriorColor = await VehicleLoockupCosmos.GetExteriorColorsAsync(vehicle?.ExteriorColorCode, vehicle?.BrandID);
        var interiorColor = await VehicleLoockupCosmos.GetInteriorColorsAsync(vehicle?.InteriorColorCode, vehicle?.BrandID);

        //if (vtModel is not null)
        {
            result = new VehicleSpecificationDTO
            {
                BodyType = vehicleModel?.BodyType,
                Class = vehicleModel?.Class,
                Cylinders = vehicleModel?.Cylinders,
                Doors = vehicleModel?.Doors,
                Engine = vehicleModel?.Engine,
                EngineType = vehicleModel?.EngineType,
                Fuel = vehicleModel?.Fuel,
                LightHeavyType = vehicleModel?.LightHeavyType,
                ModelCode = vehicleModel?.ModelCode,
                ProductionDate = vehicle?.ProductionDate,
                ModelYear = vehicle?.ModelYear,
                FuelLiter = null,
                ModelDescription = vehicleModel?.ModelDescription,
                Side = vehicleModel?.Side,
                Style = vehicleModel?.Style,
                TankCap = vehicleModel?.TankCap,
                Transmission = vehicleModel?.Transmission,
                VariantDescription = vehicleModel?.VariantDescription,
                ExteriorColor = exteriorColor?.Description,
                InteriorColor = interiorColor?.Description
            };
        }

        return result;
    }
}