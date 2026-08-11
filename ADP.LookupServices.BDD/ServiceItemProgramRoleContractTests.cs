using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text.Json;
using Xunit;

namespace LookupServices.BDD;

public class ServiceItemProgramRoleContractTests
{
    [Fact]
    public void ProgramRoleDefaultsToScheduledServiceWhenOmitted()
    {
        var direct = new ServiceItemModel();
        var deserialized = JsonSerializer.Deserialize<ServiceItemModel>("{}");

        Assert.Equal(ServiceItemProgramRole.ScheduledService, direct.ProgramRole);
        Assert.NotNull(deserialized);
        Assert.Equal(ServiceItemProgramRole.ScheduledService, deserialized!.ProgramRole);
    }

    [Fact]
    public void RewardProgramRoleRoundTripsAsCatalogMetadata()
    {
        var json = JsonSerializer.Serialize(new ServiceItemModel
        {
            ProgramRole = ServiceItemProgramRole.Reward,
        });

        var roundTripped = JsonSerializer.Deserialize<ServiceItemModel>(json);

        Assert.Contains("\"ProgramRole\":\"Reward\"", json, StringComparison.Ordinal);
        Assert.NotNull(roundTripped);
        Assert.Equal(ServiceItemProgramRole.Reward, roundTripped!.ProgramRole);
    }

    [Fact]
    public void EvaluatedVehicleServiceItemDtoDoesNotExposeProgramRole()
    {
        Assert.Null(typeof(VehicleServiceItemDTO).GetProperty(nameof(ServiceItemModel.ProgramRole)));
    }
}
