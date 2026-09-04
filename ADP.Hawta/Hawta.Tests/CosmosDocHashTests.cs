using System.Text.Json.Nodes;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class CosmosDocHashTests
{
    private static CosmosDocument Doc(IDictionary<string, object?> body) => new()
    {
        Id = "K",
        PartitionKey = ["K"],
        Body = body,
    };

    [Fact]
    public void NumbersHashByValue_NotByText()
    {
        // The written side carries SQL-scale decimals (1.500, 135000.00); Cosmos re-renders
        // them on read (1.5, 135000). The canonical hash must not care.
        var expected = CosmosDocHash.Compute(Doc(new Dictionary<string, object?>
        {
            ["Quantity"] = 1.500m,
            ["Price"] = 135000.00m,
            ["Count"] = 3,
        }));

        var actual = CosmosDocHash.Compute(new JsonObject
        {
            ["id"] = "K",
            ["Quantity"] = JsonNode.Parse("1.5"),
            ["Price"] = JsonNode.Parse("135000"),
            ["Count"] = JsonNode.Parse("3.0"),   // even a float-rendered integer
        });

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExponentAndPlainForms_HashAlike()
    {
        var a = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"v": 15E-1}""")!);
        var b = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"v": 1.5}""")!);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentValues_StillDiffer()
    {
        var a = CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["v"] = 1.5m }));
        var b = CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["v"] = 1.51m }));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void OffsetDateTimes_HashByInstant_AtMicrosecondPrecision()
    {
        // The app serializes the DateTimeOffset it was given; the snapshot stores the same instant
        // as a UTC TIMESTAMP with DuckDB's microseconds (ticks truncated). One hash for all three.
        var fromClr = CosmosDocHash.Compute(Doc(new Dictionary<string, object?>
        {
            ["ClaimDate"] = new DateTimeOffset(2025, 5, 3, 9, 3, 44, TimeSpan.FromHours(5)).AddTicks(9551234),
        }));
        var fromUtcText = CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["ClaimDate"] = "2025-05-03T04:03:44.955123Z" });
        var fromOffsetText = CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["ClaimDate"] = "2025-05-03T04:03:44.9551239+00:00" });

        Assert.Equal(fromClr, fromUtcText);
        Assert.Equal(fromClr, fromOffsetText);

        // Trailing-zero trimming (System.Text.Json renders .955120 as .95512) is a rendering, not a value.
        Assert.Equal(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-01-01T00:00:00.95512Z" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-01-01T00:00:00.955120+00:00" }));
    }

    [Fact]
    public void OffsetDateTimes_StillDifferByAMicrosecond()
    {
        Assert.NotEqual(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2025-05-03T04:03:44.955123Z" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2025-05-03T04:03:44.955124Z" }));
    }

    [Fact]
    public void DateTimesWithoutOffset_LoseTheSeventhDigit_ButKeepTheirText()
    {
        // datetime2(7) through the app vs the same value through the snapshot's microseconds.
        Assert.Equal(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["ProcessDate"] = "2025-04-17T12:03:14.1078611" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["ProcessDate"] = "2025-04-17T12:03:14.107861" }));
        // A rendered-without-fraction value and its zero-fraction twin are one value.
        Assert.Equal(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2024-07-01T00:00:00" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2024-07-01T00:00:00.000" }));
        // Still a microsecond apart is still different.
        Assert.NotEqual(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2025-04-17T12:03:14.107861" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2025-04-17T12:03:14.107862" }));
    }

    [Fact]
    public void DateTimesWithoutOffset_AndBareDates_AreComparedAsWritten()
    {
        // No offset means no instant to agree on: the text is the value, and it is NOT the same
        // value as its UTC-suffixed twin (consumers parse the two differently).
        Assert.NotEqual(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-07-19T13:06:08" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-07-19T13:06:08Z" }));
        Assert.NotEqual(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-07-19" }),
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-07-19T00:00:00Z" }));
        Assert.Equal(
            CosmosDocHash.Compute(new JsonObject { ["id"] = "K", ["At"] = "2026-07-19T13:06:08" }),
            CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["At"] = new DateTime(2026, 7, 19, 13, 6, 8) })));
    }

    [Fact]
    public void NumericLookingStrings_AreNotNumbers()
    {
        var text = CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["v"] = "1.50" }));
        var number = CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["v"] = 1.50m }));
        Assert.NotEqual(text, number);

        // And a numeric string keeps its exact text (no value-normalization of strings).
        var padded = CosmosDocHash.Compute(Doc(new Dictionary<string, object?> { ["v"] = "1.5" }));
        Assert.NotEqual(text, padded);
    }

    [Fact]
    public void NestedArraysAndObjects_NormalizeRecursively()
    {
        var expected = CosmosDocHash.Compute(Doc(new Dictionary<string, object?>
        {
            ["Labors"] = new List<object?>
            {
                new Dictionary<string, object?> { ["LaborCode"] = "081099", ["LaborHour"] = 0.50 },
            },
        }));

        var actual = CosmosDocHash.Compute((JsonObject)JsonNode.Parse(
            """{"id":"K","Labors":[{"LaborHour":0.5,"LaborCode":"081099"}]}""")!);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PropertyOrder_NeverMatters()
    {
        var a = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"b":1,"a":null}""")!);
        var b = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"a":null,"b":1}""")!);
        Assert.Equal(a, b);
    }

    [Fact]
    public void HugeNumbers_BeyondDecimal_StillCanonicalize()
    {
        var a = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"v": 1E+300}""")!);
        var b = CosmosDocHash.Compute((JsonObject)JsonNode.Parse("""{"v": 1.0E+300}""")!);
        Assert.Equal(a, b);
    }
}
