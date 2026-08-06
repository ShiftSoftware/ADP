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
