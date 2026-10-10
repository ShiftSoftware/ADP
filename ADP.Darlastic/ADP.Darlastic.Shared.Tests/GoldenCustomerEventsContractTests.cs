using System.Text.Json;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Shared.Tests;

/// <summary>
/// The timeline DTOs' wire contract. Event types and link kinds travel by NAME whatever the host's
/// serializer options are — staged payloads use the same names, and a consumer that switches on
/// them must not break because a host forgot a converter. Nothing on these DTOs is a protected
/// personal field: the events contract carries none.
/// </summary>
public class GoldenCustomerEventsContractTests
{
    [Fact]
    public void EventTypeAndLink_SerializeByName_WithDefaultOptions()
    {
        var dto = new GoldenCustomerEventDTO
        {
            Type = GoldenEventType.SafetyRecall,
            EventID = "SYNTHVIN000000001|CAMPAIGN-1",
            Date = null,
            Link = GoldenEventLink.VinOwner,
            Title = "CAMPAIGN-1 Synthetic campaign",
        };

        string json = JsonSerializer.Serialize(dto);

        Assert.Contains("\"Type\":\"SafetyRecall\"", json);
        Assert.Contains("\"Link\":\"VinOwner\"", json);
        var back = JsonSerializer.Deserialize<GoldenCustomerEventDTO>(json)!;
        Assert.Equal(GoldenEventType.SafetyRecall, back.Type);
        Assert.Equal(GoldenEventLink.VinOwner, back.Link);
    }

    [Fact]
    public void EventDate_IsAPlainCalendarDate()
    {
        var dto = new GoldenCustomerEventDTO { Type = GoldenEventType.ServiceVisit, EventID = "x", Date = new DateOnly(2026, 2, 1) };

        Assert.Contains("\"Date\":\"2026-02-01\"", JsonSerializer.Serialize(dto));
    }

    [Theory]
    [InlineData(typeof(GoldenCustomerEventsDTO))]
    [InlineData(typeof(GoldenCustomerEventDTO))]
    [InlineData(typeof(GoldenCustomerVehiclesDTO))]
    [InlineData(typeof(GoldenCustomerVehicleDTO))]
    public void TimelineDtos_CarryNoProtectedFields(Type dto)
    {
        Assert.DoesNotContain(dto.GetProperties(),
            p => p.PropertyType == typeof(ShiftSoftware.ShiftEntity.Model.Dtos.PiiFieldDTO));
    }
}
