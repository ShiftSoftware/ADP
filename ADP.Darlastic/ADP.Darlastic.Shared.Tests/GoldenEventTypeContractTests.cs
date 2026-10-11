using System.Text.Json;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Shared.Tests;

/// <summary>
/// The timeline vocabulary is append-only: staged payloads and API responses carry event types and
/// link kinds by NAME, and a renumbered or renamed member would silently relabel history. These
/// tests pin every member's number and name and prove each one round-trips through JSON by name.
/// </summary>
public class GoldenEventTypeContractTests
{
    [Fact]
    public void EventTypes_KeepTheirNumbersAndNames()
    {
        var expected = new (GoldenEventType Type, int Number, string Name)[]
        {
            (GoldenEventType.ServiceVisit, 1, "ServiceVisit"),
            (GoldenEventType.Inspection, 2, "Inspection"),
            (GoldenEventType.WarrantyClaim, 3, "WarrantyClaim"),
            (GoldenEventType.SafetyRecall, 4, "SafetyRecall"),
            (GoldenEventType.Activation, 5, "Activation"),
            (GoldenEventType.VehicleSale, 6, "VehicleSale"),
            (GoldenEventType.ServiceBooking, 7, "ServiceBooking"),
            (GoldenEventType.TestDrive, 8, "TestDrive"),
            (GoldenEventType.Quotation, 9, "Quotation"),
            (GoldenEventType.AppRegistration, 10, "AppRegistration"),
        };

        Assert.Equal(expected.Length, Enum.GetValues<GoldenEventType>().Length);
        foreach (var (type, number, name) in expected)
        {
            Assert.Equal(number, (int)type);
            Assert.Equal(name, type.ToString());
        }
    }

    [Fact]
    public void LinkKinds_KeepTheirNumbersAndNames()
    {
        Assert.Equal(
            ["Profile=1", "VinOwner=2", "VinContact=3", "VinPreDelivery=4"],
            Enum.GetValues<GoldenEventLink>().Select(l => $"{l}={(int)l}").ToArray());
    }

    public static TheoryData<GoldenEventType> AllEventTypes()
    {
        var data = new TheoryData<GoldenEventType>();
        foreach (var t in Enum.GetValues<GoldenEventType>()) data.Add(t);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllEventTypes))]
    public void EveryEventType_SerializesAndDeserializesByName(GoldenEventType type)
    {
        var dto = new GoldenCustomerEventDTO { Type = type, EventID = "X-1", Link = GoldenEventLink.VinPreDelivery };

        string json = JsonSerializer.Serialize(dto);

        Assert.Contains($"\"Type\":\"{type}\"", json);
        Assert.Contains("\"Link\":\"VinPreDelivery\"", json);
        var back = JsonSerializer.Deserialize<GoldenCustomerEventDTO>(json)!;
        Assert.Equal(type, back.Type);
        Assert.Equal(GoldenEventLink.VinPreDelivery, back.Link);
        Assert.Equal(type, JsonSerializer.Deserialize<GoldenEventType>($"\"{type}\""));
    }
}
