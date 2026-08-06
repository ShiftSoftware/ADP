using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class TypedSnapshotTableDefinitionTests
{
    private sealed class ExampleRow
    {
        public string? Code { get; set; }

        [SnapshotDecimal(18, 3)]
        public decimal? Quantity { get; set; }

        public DateTime? ChangedAt { get; set; }
    }

    [Fact]
    public void Model_DrivesSchemaColumnNamesTypesAndOrder()
    {
        var table = new SnapshotTableDefinition<ExampleRow>("Example");

        Assert.Equal(["Code", "Quantity", "ChangedAt"], table.Columns.Select(column => column.Name));
        Assert.Equal(["VARCHAR", "DECIMAL(18,3)", "TIMESTAMP"], table.Columns.Select(column => column.DuckDbType));
        Assert.Equal("Quantity", table.Column(row => row.Quantity));
    }

    [Fact]
    public void Read_UsesTheTypedContract()
    {
        var table = new SnapshotTableDefinition<ExampleRow>("Example");
        var stored = new DirtyRow(
            "A", DateTime.UtcNow, false, null,
            new Dictionary<string, object?>
            {
                ["Code"] = "X",
                ["Quantity"] = 12.5m,
                ["ChangedAt"] = new DateTime(2026, 8, 6, 1, 2, 3),
            });

        var row = table.Read(stored);

        Assert.Equal("X", row.Code);
        Assert.Equal(12.5m, row.Quantity);
        Assert.Equal(new DateTime(2026, 8, 6, 1, 2, 3), row.ChangedAt);
    }

    private sealed class UnboundedDecimalRow
    {
        public decimal Value { get; set; }
    }

    [Fact]
    public void DecimalModel_RequiresAnExplicitStorageShape()
    {
        var exception = Assert.Throws<TypeInitializationException>(
            () => new SnapshotTableDefinition<UnboundedDecimalRow>("Invalid"));

        Assert.Contains("SnapshotDecimal", exception.InnerException?.Message);
    }

    private sealed class ExampleDocument
    {
        public string id { get; set; } = "model-id-must-not-win";
        public string? Code { get; set; }
        public decimal? Quantity { get; set; }
    }

    [Fact]
    public void CosmosDocument_FromModel_UsesTypedPropertiesAndAuthoritativeCoordinates()
    {
        var document = CosmosDocument.FromModel(
            "coordinate-id",
            ["partition"],
            new ExampleDocument { Code = "A", Quantity = 1.25m });

        Assert.Equal("coordinate-id", document.Id);
        Assert.Single(document.PartitionKey);
        Assert.Equal("partition", document.PartitionKey[0]);
        Assert.DoesNotContain("id", document.Body.Keys);
        Assert.Equal(["Code", "Quantity"], document.Body.Keys);
        Assert.Equal("A", ((System.Text.Json.JsonElement)document.Body["Code"]!).GetString());
        Assert.Equal(1.25m, ((System.Text.Json.JsonElement)document.Body["Quantity"]!).GetDecimal());
    }
}
