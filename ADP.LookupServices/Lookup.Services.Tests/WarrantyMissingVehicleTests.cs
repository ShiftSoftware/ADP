using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class WarrantyMissingVehicleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Independent_warranty_evidence_does_not_require_a_vehicle_entry(bool dateShift)
    {
        var start = new DateTime(2026, 1, 15);
        var aggregate = new CompanyDataAggregateModel();
        if (dateShift)
            aggregate.WarrantyDateShifts.Add(new() { NewDate = start });
        else
            aggregate.VehicleServiceActivations.Add(new() { WarrantyActivationDate = start });

        var result = new WarrantyAndFreeServiceDateEvaluator(aggregate, new LookupOptions())
            .Evaluate(null!, null!, false, false);

        Assert.Equal(start, result.WarrantyStartDate);
        Assert.Equal(start.AddYears(3), result.WarrantyEndDate);
        Assert.Equal(dateShift ? (DateTime?)null : start, result.FreeServiceStartDate);
    }

    [Fact]
    public void Known_brand_keeps_its_configured_warranty_duration()
    {
        var start = new DateTime(2026, 1, 15);
        var aggregate = new CompanyDataAggregateModel();
        aggregate.VehicleServiceActivations.Add(new() { WarrantyActivationDate = start });
        var options = new LookupOptions();
        options.BrandStandardWarrantyPeriodsInYears[7] = 5;

        var result = new WarrantyAndFreeServiceDateEvaluator(aggregate, options)
            .Evaluate(new VehicleEntryModel { BrandID = 7 }, null!, false, false);

        Assert.Equal(start.AddYears(5), result.WarrantyEndDate);
    }
}
