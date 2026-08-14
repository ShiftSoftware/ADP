using System.Text.Json;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// A published two-table snapshot plus a separate export target: the exporter's contract needs
/// a real publish directory to project from, and a second table to prove that a table which did
/// not change keeps its older publish id in the handoff.
/// </summary>
public sealed class ExporterFixture : IDisposable
{
    public SnapshotStore Store { get; }
    public SnapshotTableDefinition Widget { get; }
    public SnapshotTableDefinition Gadget { get; }
    public string PublishDirectory { get; }
    public string ExportDirectory { get; }

    public const string SnapshotName = "test-read";
    public const string ManifestFileName = "powerbi-manifest.json";

    public ExporterFixture()
    {
        Store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        Widget = new SnapshotTableDefinition("Widget",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
        ]);
        Gadget = new SnapshotTableDefinition("Gadget",
        [
            new SnapshotColumn("Label", "VARCHAR"),
        ]);
        Store.EnsureTable(Widget);
        Store.EnsureTable(Gadget);

        var root = Path.Combine(Path.GetTempPath(), $"hawta-export-tests-{Guid.NewGuid():N}");
        PublishDirectory = Path.Combine(root, "publish");
        ExportDirectory = Path.Combine(root, "handoff");
    }

    public void MergeWidgets(params (string Key, string Code, int Quantity)[] rows)
    {
        var staging = Store.CreateStagingTable(Widget);
        foreach (var row in rows)
        {
            Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash")
                SELECT "Code", "Quantity", ?, {RowHash.Expression(["Code", "Quantity"])}
                FROM (SELECT ? AS "Code", ? AS "Quantity")
                """,
                row.Key, row.Code, row.Quantity);
        }
        SnapshotMerge.Execute(Store, Widget, staging, new SnapshotMergeOptions { Source = "test", DeletesEnabled = true });
    }

    public void MergeGadgets(params (string Key, string Label)[] rows)
    {
        var staging = Store.CreateStagingTable(Gadget);
        foreach (var row in rows)
        {
            Store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName} ("Label", "_PrimaryKey", "_RowHash")
                SELECT "Label", ?, {RowHash.Expression(["Label"])}
                FROM (SELECT ? AS "Label")
                """,
                row.Key, row.Label);
        }
        SnapshotMerge.Execute(Store, Gadget, staging, new SnapshotMergeOptions { Source = "test", DeletesEnabled = true });
    }

    public SnapshotPublishResult Publish() =>
        SnapshotPublisher.Publish(Store, new SnapshotPublishOptions
        {
            PublishDirectory = PublishDirectory,
            SnapshotName = SnapshotName,
            Tables = [Widget, Gadget],
        });

    public SnapshotExportResult Export(
        IReadOnlyList<string>? tables = null,
        int keepVersionsPerTable = 3,
        Action? onBeforeManifestCommit = null,
        Func<string, bool>? retentionDelete = null) =>
        SnapshotExporter.Export(new SnapshotExportOptions
        {
            PublishDirectory = PublishDirectory,
            ExportDirectory = ExportDirectory,
            SnapshotName = SnapshotName,
            Tables = tables,
            KeepVersionsPerTable = keepVersionsPerTable,
            OnBeforeManifestCommit = onBeforeManifestCommit,
            RetentionDelete = retentionDelete,
        });

    public SnapshotExportManifest ReadManifest()
    {
        var path = Path.Combine(ExportDirectory, ManifestFileName);
        return JsonSerializer.Deserialize<SnapshotExportManifest>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Manifest deserialized to null.");
    }

    public string[] Versions(string table)
    {
        var folder = Path.Combine(ExportDirectory, table);
        return Directory.Exists(folder)
            ? [.. Directory.GetFiles(folder, "*.parquet").Select(Path.GetFileName).Order()!]
            : [];
    }

    public string[] StagingFiles() =>
        Directory.Exists(ExportDirectory)
            ? [.. Directory.GetFiles(ExportDirectory, "*.staging", SearchOption.AllDirectories).Select(Path.GetFileName)!]
            : [];

    public void Dispose()
    {
        Store.Dispose();
        var root = Path.GetDirectoryName(PublishDirectory)!;
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}

public class SnapshotExporterTests : IDisposable
{
    private readonly ExporterFixture fx = new();

    [Fact]
    public void Export_LaysOutOneFolderPerTable_WithTheManifestNamingTheCurrentVersion()
    {
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.MergeGadgets(("G1", "one"));
        var publish = fx.Publish();

        var result = fx.Export();

        Assert.Equal(SnapshotExportStatus.Exported, result.Status);
        Assert.Equal(publish.PublishId, result.AsOfPublishId);
        // Alphabetical, not registry order: the exporter reads the shim manifest, which is
        // ORDER BY "Table" — so the handoff's table order is stable across config changes.
        Assert.Equal(["Gadget", "Widget"], result.TablesWritten);
        Assert.Empty(result.TablesReused);
        Assert.Empty(fx.StagingFiles());

        Assert.Equal([$"{publish.PublishId}.parquet"], fx.Versions("Widget"));
        Assert.Equal([$"{publish.PublishId}.parquet"], fx.Versions("Gadget"));

        var manifest = fx.ReadManifest();
        Assert.Equal(SnapshotExporter.ManifestVersion, manifest.ManifestVersion);
        Assert.Equal(ExporterFixture.SnapshotName, manifest.SnapshotName);
        Assert.Equal("latest-per-table", manifest.SelectionMode);
        Assert.Equal(publish.PublishId, manifest.AsOfPublishId);
        Assert.Equal(".", manifest.PathBase);

        var widget = manifest.Tables.Single(t => t.Table == "Widget");
        Assert.Equal($"Widget/{publish.PublishId}.parquet", widget.ParquetPath);
        Assert.Equal(publish.PublishId, widget.PublishId);
        Assert.Equal(2, widget.RowCount);
    }

    [Fact]
    public void ManifestPathsResolve_AndAreForwardSlashed_SoNonWindowsConsumersCanReadThem()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export();

        foreach (var entry in fx.ReadManifest().Tables)
        {
            Assert.DoesNotContain('\\', entry.ParquetPath);
            Assert.StartsWith($"{entry.Table}/", entry.ParquetPath);

            var resolved = Path.Combine(fx.ExportDirectory, entry.ParquetPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(resolved), $"Manifest names '{entry.ParquetPath}', which is not on disk.");
        }
    }

    [Fact]
    public void UnchangedTable_KeepsItsOlderPublishId_WhileTheChangedOneAdvances()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();
        fx.Export();

        // Only Widget changes, so the publisher re-exports only Widget's parquet.
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        var second = fx.Publish();
        var result = fx.Export();

        Assert.NotEqual(first.PublishId, second.PublishId);
        Assert.Equal(["Widget"], result.TablesWritten);
        Assert.Equal(["Gadget"], result.TablesReused);

        var manifest = fx.ReadManifest();
        Assert.Equal(second.PublishId, manifest.AsOfPublishId);
        Assert.Equal(second.PublishId, manifest.Tables.Single(t => t.Table == "Widget").PublishId);
        // The whole point of latest-per-table: an unchanged table is not re-versioned or re-copied.
        Assert.Equal(first.PublishId, manifest.Tables.Single(t => t.Table == "Gadget").PublishId);
        Assert.Equal([$"{first.PublishId}.parquet"], fx.Versions("Gadget"));
    }

    [Fact]
    public void ReExportOfTheSamePublish_IsSkippedEntirely()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export();

        var result = fx.Export();

        Assert.Equal(SnapshotExportStatus.SkippedUpToDate, result.Status);
        Assert.Empty(result.TablesWritten);
        Assert.Equal(["Gadget", "Widget"], result.TablesReused);
    }

    [Fact]
    public void AMissingTargetFile_DefeatsTheUpToDateShortCircuit()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var publish = fx.Publish();
        fx.Export();

        // An interrupted upload, or a consumer-side deletion: the manifest still names this
        // publish, so a naive id comparison would call the handoff complete.
        File.Delete(Path.Combine(fx.ExportDirectory, "Widget", $"{publish.PublishId}.parquet"));

        var result = fx.Export();

        Assert.Equal(SnapshotExportStatus.Exported, result.Status);
        Assert.Equal(["Widget"], result.TablesWritten);
        Assert.True(File.Exists(Path.Combine(fx.ExportDirectory, "Widget", $"{publish.PublishId}.parquet")));
    }

    [Fact]
    public void EmptyTables_AreExportedAndNamed_NotHidden()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        // Gadget is never merged — the shape an un-wired CSV family has in production.
        fx.Publish();

        var result = fx.Export();

        Assert.Equal(["Gadget"], result.TablesWithNoRows);

        var gadget = fx.ReadManifest().Tables.Single(t => t.Table == "Gadget");
        Assert.Equal(0, gadget.RowCount);
        // Exported, not omitted: the consumer is told the table exists AND that it is empty.
        Assert.True(File.Exists(Path.Combine(fx.ExportDirectory, "Gadget", $"{gadget.PublishId}.parquet")));
    }

    [Fact]
    public void CrashBeforeTheManifestCommit_LeavesTheStandingManifestIntact()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var first = fx.Publish();
        fx.Export();

        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));
        fx.Publish();

        Assert.Throws<IOException>(() => fx.Export(onBeforeManifestCommit: () => throw new IOException("share offline")));

        // The consumer still resolves the previous complete handoff, and its files are all there.
        var manifest = fx.ReadManifest();
        Assert.Equal(first.PublishId, manifest.AsOfPublishId);
        foreach (var entry in manifest.Tables)
            Assert.True(File.Exists(Path.Combine(fx.ExportDirectory, entry.ParquetPath.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public void StaleStagingFiles_AreClearedByTheNextExport()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export();

        Directory.CreateDirectory(Path.Combine(fx.ExportDirectory, "Widget"));
        File.WriteAllText(Path.Combine(fx.ExportDirectory, "Widget", "20260101000000000.parquet.staging"), "torn");
        File.WriteAllText(Path.Combine(fx.ExportDirectory, $"{ExporterFixture.ManifestFileName}.staging"), "torn");

        fx.MergeWidgets(("W1", "alpha", 9));
        fx.Publish();
        fx.Export();

        Assert.Empty(fx.StagingFiles());
    }

    [Fact]
    public void Retention_KeepsTheNewestVersionsPerTable()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export(keepVersionsPerTable: 2);

        for (var i = 2; i <= 4; i++)
        {
            fx.MergeWidgets(("W1", "alpha", i));
            fx.Publish();
            fx.Export(keepVersionsPerTable: 2);
        }

        var widgetVersions = fx.Versions("Widget");
        Assert.Equal(2, widgetVersions.Length);
        // The current version is the newest, and the one before it survives so a consumer that
        // read the previous manifest can still fetch its files.
        Assert.Equal(fx.ReadManifest().Tables.Single(t => t.Table == "Widget").PublishId,
            Path.GetFileNameWithoutExtension(widgetVersions[^1]));
    }

    [Fact]
    public void RetentionCannotDeleteTheCurrentVersion_EvenAtTheMinimumKeep()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export(keepVersionsPerTable: 2);

        for (var i = 2; i <= 5; i++)
        {
            fx.MergeWidgets(("W1", "alpha", i));
            fx.Publish();
            var result = fx.Export(keepVersionsPerTable: 2);

            var current = fx.ReadManifest().Tables.Single(t => t.Table == "Widget");
            Assert.True(File.Exists(Path.Combine(fx.ExportDirectory, "Widget", $"{current.PublishId}.parquet")),
                $"Retention deleted the version the manifest names ({current.PublishId}).");
            Assert.Equal(0, result.FilesSkippedByRetention);
        }
    }

    [Fact]
    public void AHeldOpenFile_IsSkippedNotLost_AndRetriedNextExport()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();
        fx.Export(keepVersionsPerTable: 2);

        for (var i = 2; i <= 4; i++)
        {
            fx.MergeWidgets(("W1", "alpha", i));
            fx.Publish();
            fx.Export(keepVersionsPerTable: 2, retentionDelete: _ => throw new IOException("held open by a consumer"));
        }

        // Nothing was deleted, but nothing was lost either — and the next clean pass catches up.
        Assert.True(fx.Versions("Widget").Length > 2);

        fx.MergeWidgets(("W1", "alpha", 99));
        fx.Publish();
        var recovered = fx.Export(keepVersionsPerTable: 2);

        Assert.Equal(2, fx.Versions("Widget").Length);
        Assert.True(recovered.FilesDeleted > 0);
    }

    [Fact]
    public void AllowlistSelectsTables_AndATypoThrowsRatherThanShippingAShortHandoff()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        fx.Publish();

        var result = fx.Export(tables: ["Widget"]);

        Assert.Equal(["Widget"], result.TablesWritten);
        Assert.Equal(["Widget"], fx.ReadManifest().Tables.Select(t => t.Table));
        Assert.False(Directory.Exists(Path.Combine(fx.ExportDirectory, "Gadget")));

        var typo = Assert.Throws<ArgumentException>(() => fx.Export(tables: ["Widgit"]));
        Assert.Contains("Widgit", typo.Message);
    }

    [Fact]
    public void NothingPublished_IsSkippedRatherThanWritingAnEmptyManifest()
    {
        var result = fx.Export();

        Assert.Equal(SnapshotExportStatus.SkippedNoPublish, result.Status);
        Assert.Null(result.AsOfPublishId);
        Assert.False(File.Exists(Path.Combine(fx.ExportDirectory, ExporterFixture.ManifestFileName)));
    }

    [Fact]
    public void ExportingIntoThePublishDirectory_IsRefused()
    {
        var same = Assert.Throws<ArgumentException>(() => SnapshotExporter.Export(new SnapshotExportOptions
        {
            PublishDirectory = fx.PublishDirectory,
            ExportDirectory = Path.Combine(fx.PublishDirectory, "."),
            SnapshotName = ExporterFixture.SnapshotName,
        }));

        Assert.Contains("must differ", same.Message);
    }

    [Fact]
    public void KeepingFewerThanTwoVersions_IsRefused()
    {
        var tooFew = Assert.Throws<ArgumentException>(() => fx.Export(keepVersionsPerTable: 1));
        Assert.Contains("at least 2", tooFew.Message);
    }

    public void Dispose() => fx.Dispose();
}
