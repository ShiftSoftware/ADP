using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class TypedSnapshotTableDefinitionTests
{
    private sealed class ExampleRow
    {
        public string? Code { get; set; }

        [SnapshotDecimal(18, 3)]
        public decimal? Quantity { get; set; }

        [SnapshotIgnoreForReplication]
        public DateTime? ChangedAt { get; set; }
    }

    [Fact]
    public void Model_DrivesSchemaColumnNamesTypesAndOrder()
    {
        var table = new SnapshotTableDefinition<ExampleRow>("Example");

        Assert.Equal(["Code", "Quantity", "ChangedAt"], table.Columns.Select(column => column.Name));
        Assert.Equal(["Code", "Quantity"], table.ReplicationColumns.Select(column => column.Name));
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
        // At construction, not at type initialization: the storage shape is now a per-definition
        // policy, so the refusal names the property the moment the table is declared.
        var exception = Assert.Throws<InvalidOperationException>(
            () => new SnapshotTableDefinition<UnboundedDecimalRow>("Invalid"));

        Assert.Contains("SnapshotDecimal", exception.Message);
    }

    private enum Colour { Red = 1, Blue = 2 }

    private sealed class Nested
    {
        public string? Note { get; set; }
    }

    private sealed class ExternalModel
    {
        public string? id { get; set; }
        public decimal? Amount { get; set; }
        public Colour? Paint { get; set; }
        public DateTimeOffset? At { get; set; }
        public Nested? Detail { get; set; }
        public List<string>? Tags { get; set; }
        public string ItemType => "External";
    }

    [Fact]
    public void ExternalModelPolicy_TypesEveryShape_AndReadsItBack()
    {
        var table = new SnapshotTableDefinition<ExternalModel>("External", SnapshotTypedTableOptions.ForExternalModel);

        Assert.Equal(["id", "Amount", "Paint", "At", "Detail", "Tags"], table.Columns.Select(column => column.Name));
        Assert.Equal(["VARCHAR", "DECIMAL(18,6)", "INTEGER", "TIMESTAMP", "VARCHAR", "VARCHAR"], table.Columns.Select(column => column.DuckDbType));
        Assert.Equal(["Detail", "Tags"], table.JsonColumns.Order());

        var row = table.Read(new DirtyRow("k", DateTime.UtcNow, false, null, new Dictionary<string, object?>
        {
            ["id"] = "k",
            ["Amount"] = 12.500000m,
            ["Paint"] = 2,
            ["At"] = new DateTime(2026, 9, 3, 8, 0, 0),
            ["Detail"] = """{"note":"hello"}""",
            ["Tags"] = """["a","b"]""",
        }));

        Assert.Equal(12.5m, row.Amount);
        Assert.Equal("12.5", row.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(Colour.Blue, row.Paint);
        Assert.Equal(new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), row.At);
        Assert.Equal("hello", row.Detail!.Note);
        Assert.Equal(["a", "b"], row.Tags!);
    }

    [Fact]
    public void ExternalModelPolicy_HonoursDecimalOverridesAndExclusions()
    {
        var options = SnapshotTypedTableOptions.ForExternalModel
            .WithDecimal(nameof(ExternalModel.Amount), 12, 2)
            .Excluding(nameof(ExternalModel.Tags));
        var table = new SnapshotTableDefinition<ExternalModel>("External", options);

        Assert.Equal("DECIMAL(12,2)", table.Columns.Single(column => column.Name == "Amount").DuckDbType);
        Assert.DoesNotContain(table.Columns, column => column.Name == "Tags");
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
