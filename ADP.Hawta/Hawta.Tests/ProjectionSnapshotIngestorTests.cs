using System.Text.Json;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// A projection source reads other snapshot tables and fills a serving table. These pin the
/// contract that makes it safe to run one on every cadence tick: it merges like any source, it
/// skips when nothing upstream moved, it reads a Deferred input from its published copy without
/// hydrating it, and it fails loudly — with a run record — when its SQL no longer binds.
/// </summary>
public sealed class ProjectionSnapshotIngestorTests : IDisposable
{
    private readonly SnapshotStore store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });

    // Source-shaped inputs: a vehicle table and a model table, keyed the way an app table is.
    private sealed class VehicleRow
    {
        public long? ID { get; set; }
        public string? VIN { get; set; }
        public long? ModelID { get; set; }
        public bool? IsDeleted { get; set; }
    }

    private sealed class ModelRow
    {
        public long? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    // The canonical model a serving table is typed from — an external class, so it uses the
    // lenient policy: an enum, a decimal without an attribute, and a nested object.
    public enum SaleKind { Retail = 1, Fleet = 2 }

    public sealed class SaleLeg
    {
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
    }

    public sealed class ServingVehicle
    {
        public string id { get; set; } = default!;
        public string? VIN { get; set; }
        public string? ModelCode { get; set; }
        public string? ModelDescription { get; set; }
        public SaleKind? Kind { get; set; }
        public decimal? Price { get; set; }
        public DateTimeOffset? SoldAt { get; set; }
        public SaleLeg? Distributor { get; set; }
    }

    private readonly SnapshotTableDefinition<VehicleRow> vehicles = new("VehiclesDbVehicle");
    private readonly SnapshotTableDefinition<ModelRow> models = new("VehiclesDbModel");
    private readonly SnapshotTableDefinition<ServingVehicle> serving =
        new("ServingVehicleEntry", SnapshotTypedTableOptions.ForExternalModel);

    private const string Sql =
        """
        SELECT v."VIN" AS "id",
               v."VIN",
               m."Code" AS "ModelCode",
               m."Name" AS "ModelDescription",
               1 AS "Kind",
               12.5 AS "Price",
               TIMESTAMP '2026-09-03 08:00:00' AS "SoldAt",
               to_json(struct_pack(InvoiceNumber := 'INV-' || v."VIN", InvoiceDate := NULL)) AS "Distributor",
               greatest(v."_LastModified", m."_LastModified") AS "SourceModified"
        FROM {VehiclesDbVehicle} v
        LEFT JOIN {VehiclesDbModel} m ON m."ID" = v."ModelID"
        WHERE v."IsDeleted" = false
        """;

    public ProjectionSnapshotIngestorTests()
    {
        store.EnsureTable(vehicles);
        store.EnsureTable(models);
        store.EnsureTable(serving);
    }

    private ProjectionSnapshotIngestorOptions Options(string sql = Sql, string? ingestVersion = null) => new()
    {
        Table = serving,
        SelectSql = sql,
        Inputs = [vehicles, models],
        PrimaryKeyColumn = "VIN",
        SourceModifiedColumn = "SourceModified",
        IngestVersion = ingestVersion,
        MergeOptions = new SnapshotMergeOptions
        {
            Source = "serving-vehicle-entry",
            RecordIdentityKind = SourceRecordIdentityKind.LogicalKey,
            DeletesEnabled = true,
        },
    };

    private SnapshotMergeResult MergeVehicles(params (long Id, string Vin, long ModelId, bool Deleted)[] rows)
    {
        var staging = store.CreateStagingTable(vehicles);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("ID", "VIN", "ModelID", "IsDeleted", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "ID", "VIN", "ModelID", "IsDeleted", CAST("ID" AS VARCHAR), {RowHash.Expression(["ID", "VIN", "ModelID", "IsDeleted"])}, NULL
                FROM (SELECT ? AS "ID", ? AS "VIN", ? AS "ModelID", ? AS "IsDeleted")
                """,
                row.Id, row.Vin, row.ModelId, row.Deleted);
        }
        return SnapshotMerge.Execute(store, vehicles, staging, new SnapshotMergeOptions
        {
            Source = "sql-vehicles-vehicle", RecordIdentityKind = SourceRecordIdentityKind.DatabaseKey, DeletesEnabled = true,
        });
    }

    private SnapshotMergeResult MergeModels(params (long Id, string Code, string Name)[] rows)
    {
        var staging = store.CreateStagingTable(models);
        foreach (var row in rows)
        {
            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("ID", "Code", "Name", "_PrimaryKey", "_RowHash", "_SourceModified")
                SELECT "ID", "Code", "Name", CAST("ID" AS VARCHAR), {RowHash.Expression(["ID", "Code", "Name"])}, NULL
                FROM (SELECT ? AS "ID", ? AS "Code", ? AS "Name")
                """,
                row.Id, row.Code, row.Name);
        }
        return SnapshotMerge.Execute(store, models, staging, new SnapshotMergeOptions
        {
            Source = "sql-vehicles-model", RecordIdentityKind = SourceRecordIdentityKind.DatabaseKey, DeletesEnabled = true,
        });
    }

    private T Scalar<T>(string sql, params object?[] parameters) =>
        (T)Convert.ChangeType(store.ExecuteScalar(sql, parameters)!, typeof(T));

    [Fact]
    public void Projection_FillsTheServingTable_AndStampsInputWatermark()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false), (11, "VIN0000000000002", 1, false));

        var result = ProjectionSnapshotIngestor.Ingest(store, Options());

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RowsInserted);
        Assert.Equal("Corolla", store.ExecuteScalar(
            "SELECT \"ModelDescription\" FROM data.\"ServingVehicleEntry\" WHERE \"_PrimaryKey\" = 'VIN0000000000001'"));
        // The serving row's freshness is the source's, handed through the projection.
        Assert.Equal(
            store.ExecuteScalar("SELECT max(\"_LastModified\") FROM data.\"VehiclesDbVehicle\""),
            store.ExecuteScalar("SELECT max(\"_LastModified\") FROM data.\"ServingVehicleEntry\""));

        var stamp = store.ReadProjectionStamp("serving-vehicle-entry");
        Assert.NotNull(stamp);
        Assert.Equal(store.ReadChangeSequenceWatermark(vehicles), stamp.InputWatermark);
    }

    [Fact]
    public void UnchangedInputs_SkipTheRun_WithARunRecord()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false));
        ProjectionSnapshotIngestor.Ingest(store, Options());

        var again = ProjectionSnapshotIngestor.Ingest(store, Options());

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, again.Status);
        Assert.Equal(1, Scalar<long>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Source\" = 'serving-vehicle-entry' AND \"Status\" = 'Skipped:SourceUnchanged'"));
    }

    [Fact]
    public void AnInputChange_ReprojectsOnlyTheDifference()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false), (11, "VIN0000000000002", 1, false));
        ProjectionSnapshotIngestor.Ingest(store, Options());

        // A model rename touches every vehicle of that model; a vehicle tombstone removes one row.
        MergeModels((1, "ZRE", "Corolla Cross"));
        MergeVehicles((10, "VIN0000000000001", 1, false));

        var result = ProjectionSnapshotIngestor.Ingest(store, Options());

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(1, result.RowsTombstoned);
        Assert.Equal("Corolla Cross", store.ExecuteScalar(
            "SELECT \"ModelDescription\" FROM data.\"ServingVehicleEntry\" WHERE \"_PrimaryKey\" = 'VIN0000000000001'"));
        Assert.Equal(true, store.ExecuteScalar(
            "SELECT \"_Deleted\" FROM data.\"ServingVehicleEntry\" WHERE \"_PrimaryKey\" = 'VIN0000000000002'"));
    }

    [Fact]
    public void ADeferredInput_IsReadFromItsPublishedCopy_WithoutHydration()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false));

        // Push the model table out to parquet and record it Deferred, exactly as a cold start does.
        var directory = Path.Combine(Path.GetTempPath(), "hawta-projection", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "models.parquet").Replace('\\', '/');
        store.Execute($"COPY (SELECT * FROM data.\"VehiclesDbModel\") TO '{file}' (FORMAT parquet)");
        store.Execute("DELETE FROM data.\"VehiclesDbModel\"");
        store.MarkTableDeferred(models.Name, "test.json", [file], rowCount: 1, contentHashes: []);

        try
        {
            var result = ProjectionSnapshotIngestor.Ingest(store, Options());

            Assert.True(result.Succeeded);
            Assert.Equal("Corolla", store.ExecuteScalar(
                "SELECT \"ModelDescription\" FROM data.\"ServingVehicleEntry\" WHERE \"_PrimaryKey\" = 'VIN0000000000001'"));
            Assert.Equal(SnapshotResidency.Deferred, store.ReadResidency(models.Name));
            Assert.Contains("read_parquet", ProjectionSnapshotIngestor.ResolveSql(store, Options()));
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void TheServingRow_MaterializesAsTheModel_WithJsonEnumOffsetAndNormalizedDecimal()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false));
        ProjectionSnapshotIngestor.Ingest(store, Options());

        var stored = store.ReadDirtyRows(serving, limit: 1).Single();
        var model = serving.Read(stored);

        Assert.Equal("VIN0000000000001", model.id);
        Assert.Equal("Corolla", model.ModelDescription);
        Assert.Equal(SaleKind.Retail, model.Kind);
        Assert.Equal(12.5m, model.Price);
        Assert.Equal("12.5", model.Price!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), model.SoldAt);
        Assert.Equal("INV-VIN0000000000001", model.Distributor!.InvoiceNumber);
        Assert.Null(model.Distributor.InvoiceDate);

        // And the generic family mapping turns it into a document without a per-family delegate.
        var mapping = CosmosFamilyMapping.ForModel(serving, "VehicleEntry", "CompanyData", "Vehicles",
            id: m => m.VIN!, partitionKey: m => [m.VIN, "VehicleEntry", null]);
        var document = mapping.Map!(stored);
        Assert.Equal("VIN0000000000001", document.Id);
        Assert.Equal("INV-VIN0000000000001",
            ((JsonElement)document.Body["Distributor"]!).GetProperty("InvoiceNumber").GetString());
        Assert.Equal(1, ((JsonElement)document.Body["Kind"]!).GetInt32());
        Assert.Equal("12.5", ((JsonElement)document.Body["Price"]!).GetRawText());
    }

    [Fact]
    public void AColumnTheSqlForgets_FailsLoudly_WithAFailedRunRecord()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false));

        var broken = Sql.Replace("m.\"Name\" AS \"ModelDescription\",", "");

        Assert.ThrowsAny<Exception>(() => ProjectionSnapshotIngestor.Ingest(store, Options(broken)));
        Assert.Equal(1, Scalar<long>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Source\" = 'serving-vehicle-entry' AND \"Status\" = 'Failed:Exception'"));
        Assert.Equal(0, Scalar<long>("SELECT count(*) FROM data.\"ServingVehicleEntry\""));
    }

    [Fact]
    public void AnUndeclaredPlaceholder_OrAnUnreferencedInput_IsRefused()
    {
        var undeclared = Assert.Throws<ArgumentException>(() =>
            ProjectionSnapshotIngestor.Ingest(store, Options(Sql + " LEFT JOIN {SomethingElse} x ON false")));
        Assert.Contains("SomethingElse", undeclared.Message);

        var unreferenced = Assert.Throws<ArgumentException>(() =>
            ProjectionSnapshotIngestor.Ingest(store, Options(Sql.Replace("LEFT JOIN {VehiclesDbModel} m ON m.\"ID\" = v.\"ModelID\"",
                "LEFT JOIN (SELECT NULL AS \"ID\", NULL AS \"Code\", NULL AS \"Name\") m ON m.\"ID\" = v.\"ModelID\""))));
        Assert.Contains("VehiclesDbModel", unreferenced.Message);
    }

    [Fact]
    public void AnIngestVersionChange_ReprojectsOnce_ThenGatesAgain()
    {
        MergeModels((1, "ZRE", "Corolla"));
        MergeVehicles((10, "VIN0000000000001", 1, false));
        ProjectionSnapshotIngestor.Ingest(store, Options());

        var bumped = ProjectionSnapshotIngestor.Ingest(store, Options(ingestVersion: "v2"));
        var gated = ProjectionSnapshotIngestor.Ingest(store, Options(ingestVersion: "v2"));

        Assert.Equal(SnapshotMergeStatus.Succeeded, bumped.Status);
        Assert.Equal(0, bumped.RowsUpdated);
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, gated.Status);
    }

    public void Dispose() => store.Dispose();
}
