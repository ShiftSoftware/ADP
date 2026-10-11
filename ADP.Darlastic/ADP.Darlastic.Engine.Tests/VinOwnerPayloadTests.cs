using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The reader of the per-VIN <c>vinowner</c> artifact that the by-VIN golden lookup serves from.
/// Synthetic data only.
/// </summary>
public class VinOwnerPayloadTests
{
    private const string Vin = "SYNTHVIN000000011";

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void VinOwnerPayload_ReadsLinksInStagedOrder_AndNeverTheStagedName()
    {
        const string payload = """
            {"vin":"SYNTHVIN000000011","owners":[
              {"vin":"SYNTHVIN000000011","goldenId":100,"name":"Synthetic Person","role":"owner","from":"2024-03-01","to":"2025-03-01","sales":1,"services":2,"firstSale":"2024-03-01","lastSale":"2024-03-01","firstService":"2024-06-01","lastService":"2024-12-01","sources":["dms|A1","dms|A2"]},
              {"vin":"SYNTHVIN000000011","goldenId":200,"name":"Another Synthetic","role":"owner","from":"2025-03-01","to":"","sales":1,"services":0,"firstSale":"2025-03-01","lastSale":"2025-03-01","firstService":"","lastService":"","sources":["dms|B1"]},
              {"vin":"SYNTHVIN000000011","goldenId":300,"name":"","role":"service-contact","from":"2024-05-01","to":"2024-05-01","sales":0,"services":1,"firstSale":"","lastSale":"","firstService":"2024-05-01","lastService":"2024-05-01","sources":[]},
              {"vin":"SYNTHVIN000000011","name":"no golden id","role":"owner"},
              {"vin":"SYNTHVIN000000011","goldenId":400,"name":"no role"}
            ]}
            """;

        var (vin, links) = VinOwnerPayload.Parse(payload);

        Assert.Equal(Vin, vin);
        Assert.Equal([100L, 200L, 300L], links.Select(l => l.GoldenId).ToArray());
        Assert.Equal([VinPeriod.OwnerRole, VinPeriod.OwnerRole, VinPeriod.ContactRole], links.Select(l => l.Role).ToArray());
        Assert.Equal(D(2024, 3, 1), links[0].From);
        Assert.Equal(D(2025, 3, 1), links[0].To);
        Assert.Null(links[1].To);
        Assert.Equal(2, links[0].Services);
        Assert.Equal(["dms|A1", "dms|A2"], links[0].Sources);
        Assert.Null(links[2].FirstSale);
        Assert.Equal(D(2024, 5, 1), links[2].LastService);
        Assert.DoesNotContain(typeof(VinOwnerPayload.Link).GetProperties(), p => p.Name.Contains("Name", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Synthetic", string.Join("|", links.Select(l => l.ToString())));
    }

    [Fact]
    public void VinOwnerPayload_WithoutOwners_IsEmpty()
    {
        var (vin, links) = VinOwnerPayload.Parse("""{"vin":"SYNTHVIN000000011"}""");

        Assert.Equal(Vin, vin);
        Assert.Empty(links);
    }
}
