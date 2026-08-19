using DuckDB.NET.Data;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The run records' journey out of the write DB and into something a health framework can query.
///
/// <para><c>meta.SyncRuns</c> lives on the agent's instance-local disk by necessity, so a check
/// that reads it directly works only where the agent runs — and silently stops working anywhere
/// else, which is a check that passes because it cannot see. These pin the published path.</para>
/// </summary>
public sealed class PublishedRunRecordTests : IDisposable
{
    private readonly PublisherFixture fx = new();

    public void Dispose() => fx.Dispose();

    private PublishedSnapshot PublishAndRead()
    {
        fx.Publish();
        var manifest = PublishedSnapshot.ResolveNewest(fx.PublishDirectory, PublisherFixture.SnapshotName);
        Assert.NotNull(manifest);
        return PublishedSnapshot.Read(manifest!);
    }

    [Fact]
    public void TheManifestCarriesTheNewestRunPerSource()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeWidgets(("W1", "alpha", 1), ("W2", "beta", 2));   // a second, newer run for the same source
        fx.MergeGadgets(("G1", "one"));

        var published = PublishAndRead();

        Assert.NotNull(published.SourceRuns);
        var widget = Assert.Single(published.SourceRuns!, run => run.SourceKey == "test-widget");
        Assert.Equal("Widget", widget.TargetTable);
        Assert.Equal("Succeeded", widget.Status);

        // The NEWEST run, not the first: one row per source, and it is the latest one.
        Assert.Equal(2, widget.RowsStaged);
        Assert.Equal(1, widget.RowsInserted);

        Assert.Contains(published.SourceRuns!, run => run.SourceKey == "test-gadget");
    }

    [Fact]
    public void ASourceThatNeverRan_HasNoEntry_WhichIsTheSignal()
    {
        // The sharpest case D2c's null rule exists for: a source that throws BEFORE staging writes
        // no run record at all, so "never ran" and "crashing every tick" both present as absence —
        // and a consumer must be able to tell that from a source that ran and found nothing.
        fx.MergeWidgets(("W1", "alpha", 1));

        var published = PublishAndRead();

        Assert.DoesNotContain(published.SourceRuns ?? [], run => run.SourceKey == "test-gadget");
    }

    [Fact]
    public void AFailedRun_IsPublishedAsItsStatus_NotOmitted()
    {
        // A merge that fails DOES write a run record, and it must reach the published set as the
        // failure it was. Omitting it would make a failing source look identical to a healthy one.
        var staging = fx.Store.CreateStagingTable(fx.Widget);
        fx.Store.Execute(
            $"""
            INSERT INTO {staging.QualifiedName} ("Code", "Quantity", "_PrimaryKey", "_RowHash", "_SourceModified")
            VALUES ('alpha', 1, NULL, 'hash', NULL)
            """);
        var result = SnapshotMerge.Execute(fx.Store, fx.Widget, staging,
            new SnapshotMergeOptions { Source = "test-widget", DeletesEnabled = false });
        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);

        fx.MergeGadgets(("G1", "one"));   // something has to change for the publish to happen
        var published = PublishAndRead();

        var widget = Assert.Single(published.SourceRuns!, run => run.SourceKey == "test-widget");
        Assert.Equal("Failed:InvalidStagingRows", widget.Status);
    }

    [Fact]
    public void TheViewDatabase_ExposesTheMetaFacts_SoASqlToolCanAskThem()
    {
        fx.MergeWidgets(("W1", "alpha", 1));
        fx.MergeGadgets(("G1", "one"));
        var published = PublishAndRead();

        var databasePath = Path.Combine(fx.PublishDirectory, "views-under-test.duckdb");
        published.WriteViewDatabase(fx.PublishDirectory, databasePath);

        using var connection = new DuckDBConnection($"Data Source={databasePath};ACCESS_MODE=READ_ONLY");
        connection.Open();

        // meta.publish_info is a name consumers were already written against; it stopped resolving
        // when the published tier dropped its DuckDB shim, and a check reading it has been
        // returning a source error rather than a freshness verdict ever since.
        // To the MICROSECOND: DuckDB's TIMESTAMP holds microseconds and .NET holds 100 ns ticks, so
        // a round trip truncates. That matters enormously for the file gate's stamp, which is an
        // equality token — it is stored as BIGINT ticks for exactly this reason — and not at all
        // here, where the value is only ever compared against a multi-hour freshness window. Pinned
        // so the difference between the two jobs stays a decision rather than an accident.
        Assert.Equal(
            Truncate(published.PublishedAt),
            Truncate((DateTime)Scalar(connection, "SELECT \"PublishedAt\" FROM meta.publish_info")!));
        Assert.Equal(published.PublishId, Scalar(connection, "SELECT \"PublishId\" FROM meta.publish_info"));
        Assert.Equal(
            Convert.ToInt64(published.ChangeSequenceHighWatermark),
            Convert.ToInt64(Scalar(connection, "SELECT \"ChangeSequenceHighWatermark\" FROM meta.publish_info")));

        Assert.Equal(2L, Convert.ToInt64(Scalar(connection, "SELECT count(*) FROM meta.published_tables")));

        Assert.Equal("Succeeded", Scalar(connection,
            "SELECT \"Status\" FROM meta.sync_runs WHERE \"SourceKey\" = 'test-widget'"));

        // And the shape a health check actually issues: a source with no run yields ONE row whose
        // value is NULL, which the age assert reports as a failure rather than as "no data".
        Assert.Null(Scalar(connection,
            "SELECT max(\"StartedAt\") FROM meta.sync_runs WHERE \"SourceKey\" = 'never-configured'"));
    }

    [Fact]
    public void AManifestWithNoRunSection_StillMaterializesAnEmptyTable()
    {
        // Additive, so a set published before the section existed must still produce a database a
        // check can query. "No rows" is the check's own answer; a catalog error is not.
        fx.MergeWidgets(("W1", "alpha", 1));
        var published = PublishAndRead();

        var stripped = published with { };
        var databasePath = Path.Combine(fx.PublishDirectory, "views-legacy.duckdb");
        (stripped with { SourceRuns = null }).WriteViewDatabase(fx.PublishDirectory, databasePath);

        using var connection = new DuckDBConnection($"Data Source={databasePath};ACCESS_MODE=READ_ONLY");
        connection.Open();

        Assert.Equal(0L, Convert.ToInt64(Scalar(connection, "SELECT count(*) FROM meta.sync_runs")));
    }

    private static DateTime Truncate(DateTime value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Kind);

    private static object? Scalar(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = command.ExecuteScalar();
        return value is DBNull ? null : value;
    }
}
