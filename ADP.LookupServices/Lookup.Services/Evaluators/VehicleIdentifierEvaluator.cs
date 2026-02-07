using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleIdentifierEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;

    public VehicleIdentifierEvaluator(CompanyDataAggregateModel companyDataAggregate)
    {
        this.CompanyDataAggregate = companyDataAggregate;
    }

    public VehicleIdentifiersDTO Evaluate(VehicleEntryModel vehicle)
    {
        if (vehicle is null)
            return new VehicleIdentifiersDTO { VIN = this.CompanyDataAggregate.VIN };

        return new VehicleIdentifiersDTO
        {
            VIN = this.CompanyDataAggregate.VIN,
            Variant = vehicle.VariantCode,
            Katashiki = vehicle.Katashiki,
            Color = vehicle.ExteriorColorCode,
            Trim = vehicle.InteriorColorCode,
            BrandID = vehicle.BrandID.ToString()
        };
    }
}