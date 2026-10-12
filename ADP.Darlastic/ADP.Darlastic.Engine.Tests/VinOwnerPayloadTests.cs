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

    private static VinOwnerPayload.Link L(long golden, string role, DateOnly? from, DateOnly? to, params string[] sources) =>
        new(golden, role, from, to, role == VinPeriod.OwnerRole ? 1 : 0, sources.Length, null, null, null, null, sources);

    private static readonly IReadOnlyDictionary<long, long> NoMerges = new Dictionary<long, long>();

    [Fact]
    public void OnePerSurvivor_WithoutMerges_KeepsEveryLinkInStagedOrder()
    {
        var links = new[]
        {
            L(100, VinPeriod.OwnerRole, D(2020, 1, 1), D(2022, 1, 1)),
            L(200, VinPeriod.OwnerRole, D(2022, 1, 1), null),
            L(300, VinPeriod.ContactRole, D(2021, 5, 1), D(2021, 6, 1)),
        };

        var kept = VinOwnerPayload.OnePerSurvivor(links, NoMerges);

        Assert.Equal([100L, 200L, 300L], kept.Select(k => k.LiveId).ToArray());
        Assert.Equal(links, kept.Select(k => k.Link).ToArray());
    }

    [Fact]
    public void OnePerSurvivor_TwoMergedOwners_ListsTheSurvivorOnce_ByTheLongerPeriod_AtItsStagedPosition()
    {
        var links = new[]
        {
            L(100, VinPeriod.OwnerRole, D(2020, 1, 1), D(2020, 6, 1), "s|a"),       // ~5 months
            L(150, VinPeriod.OwnerRole, D(2020, 6, 1), D(2021, 1, 1), "s|x"),       // someone else
            L(200, VinPeriod.OwnerRole, D(2021, 1, 1), D(2024, 1, 1), "s|b"),       // 3 years
        };
        var live = new Dictionary<long, long> { [100] = 900, [200] = 900 };

        var kept = VinOwnerPayload.OnePerSurvivor(links, live);

        Assert.Equal([150L, 900L], kept.Select(k => k.LiveId).ToArray());
        Assert.Equal(200, kept[1].Link.GoldenId);
        Assert.Equal(["s|b"], kept[1].Link.Sources);                             // nothing folded in
    }

    [Fact]
    public void OnePerSurvivor_OwnerBeatsServiceContact_WhateverThePeriod()
    {
        var links = new[]
        {
            L(100, VinPeriod.OwnerRole, D(2024, 1, 1), D(2024, 2, 1)),
            L(200, VinPeriod.ContactRole, D(2015, 1, 1), D(2025, 1, 1)),
        };
        var live = new Dictionary<long, long> { [200] = 100 };

        var kept = Assert.Single(VinOwnerPayload.OnePerSurvivor(links, live));

        Assert.Equal(100L, kept.LiveId);
        Assert.Equal(VinPeriod.OwnerRole, kept.Link.Role);
    }

    [Fact]
    public void OnePerSurvivor_LinkedSurvivorItself_AndAMergedAwayGolden_ShareOneEntry()
    {
        var links = new[]
        {
            L(100, VinPeriod.ContactRole, D(2023, 1, 1), D(2023, 1, 1)),
            L(200, VinPeriod.ContactRole, D(2022, 1, 1), D(2024, 1, 1)),
        };
        var live = new Dictionary<long, long> { [100] = 200 };                    // 100 merged into 200

        var kept = Assert.Single(VinOwnerPayload.OnePerSurvivor(links, live));

        Assert.Equal(200L, kept.LiveId);
        Assert.Equal(200, kept.Link.GoldenId);
    }

    [Fact]
    public void IsStronger_OpenPeriodIsLongest_UnknownStartShortest_EqualIsNotStronger()
    {
        var open = L(1, VinPeriod.OwnerRole, D(2025, 1, 1), null);
        var decade = L(2, VinPeriod.OwnerRole, D(2010, 1, 1), D(2020, 1, 1));
        var unknownStart = L(3, VinPeriod.OwnerRole, null, D(2020, 1, 1));
        var sameDecade = L(4, VinPeriod.OwnerRole, D(2010, 1, 1), D(2020, 1, 1));

        Assert.True(VinOwnerPayload.IsStronger(open, decade));
        Assert.False(VinOwnerPayload.IsStronger(decade, open));
        Assert.True(VinOwnerPayload.IsStronger(decade, unknownStart));
        Assert.False(VinOwnerPayload.IsStronger(unknownStart, decade));
        Assert.False(VinOwnerPayload.IsStronger(decade, sameDecade));            // a tie keeps the earlier link
        Assert.False(VinOwnerPayload.IsStronger(sameDecade, decade));
    }

    [Fact]
    public void OnePerSurvivor_Tie_KeepsTheEarlierStagedLink()
    {
        var links = new[]
        {
            L(100, VinPeriod.OwnerRole, D(2020, 1, 1), D(2021, 1, 1)),
            L(200, VinPeriod.OwnerRole, D(2022, 1, 1), D(2023, 1, 1)),
        };
        var live = new Dictionary<long, long> { [100] = 500, [200] = 500 };

        var kept = Assert.Single(VinOwnerPayload.OnePerSurvivor(links, live));

        Assert.Equal(100, kept.Link.GoldenId);
    }
}
