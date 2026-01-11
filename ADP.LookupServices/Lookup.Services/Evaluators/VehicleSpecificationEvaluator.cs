using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleSpecificationEvaluator
{
    private readonly IVehicleLoockupCosmosService VehicleLoockupCosmos;
    public VehicleSpecificationEvaluator(IVehicleLoockupCosmosService vehicleLoockupCosmos)
    {
        this.VehicleLoockupCosmos = vehicleLoockupCosmos;
    }

    public async Task<VehicleSpecificationDTO> Evaluate(VehicleEntryModel vehicle)
    {
        VehicleSpecificationDTO result = new();

        var vehicleModel = vehicle?.VehicleModel;

        if (vehicleModel is null)
        {
            vehicleModel = await VehicleLoockupCosmos.GetVehicleModelsAsync(vehicle?.VariantCode, vehicle?.BrandID);

            if (vehicleModel is not null)
                VehicleLoockupCosmos.UpdateVSDataModel(vehicle, vehicleModel);
        }

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
                ExteriorColor = vehicle?.ExteriorColor?.Description,
                InteriorColor = vehicle?.InteriorColor?.Description
            };
        }

        if (vehicle?.ExteriorColor is null)
        {
            var color = await VehicleLoockupCosmos.GetExteriorColorsAsync(vehicle?.ExteriorColorCode, vehicle?.BrandID);
            if (color is not null)
            {
                result.ExteriorColor = color?.Description;
                VehicleLoockupCosmos.UpdateVSDataColor(vehicle, color);
            }
        }

        if (vehicle?.InteriorColor is null)
        {
            var trim = await VehicleLoockupCosmos.GetInteriorColorsAsync(vehicle?.InteriorColorCode, vehicle?.BrandID);
            if (trim is not null)
            {
                result.InteriorColor = trim?.Description;
                VehicleLoockupCosmos.UpdateVSDataTrim(vehicle, trim);
            }
        }

        await VehicleLoockupCosmos.SaveChangesAsync();

        return result;
    }
}