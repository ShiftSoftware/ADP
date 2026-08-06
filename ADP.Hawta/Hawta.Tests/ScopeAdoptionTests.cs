using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Cross-scope key semantics: a source whose staging carries a key currently owned by a
/// DIFFERENT <c>_SourceScope</c> ADOPTS the row (last-writer-wins on scope) — the
/// alternative is the old scope tombstoning it and the new scope resurrecting it, forever,
/// churning Cosmos with delete/re-add pairs. Adoption is visible: <c>RowsRescoped</c>.
/// </summary>
public class ScopeAdoptionTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    public void Dispose() => snapshot.Dispose();

    [Fact]
    public void SameContentUnderANewScope_IsAdopted_Rescoped_AndRestamped()
    {
        snapshot.Merge([("K", "alpha", 1)], scope: "A");
        var before = snapshot.Scalar<DateTime>(
            "SELECT \"_LastModified\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K'");

        var adoption = snapshot.Merge([("K", "alpha", 1)], scope: "B");

        Assert.Equal(1, adoption.RowsRescoped);
        Assert.Equal(1, adoption.RowsUpdated);   // same hash — the scope change alone drives the update
        Assert.Equal(0, adoption.RowsInserted);
        Assert.Equal("B", snapshot.Scalar<string>(
            "SELECT \"_SourceScope\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K'"));

        // The stamp moved forward: the scope change replicates like any other change.
        var after = snapshot.Scalar<DateTime>(
            "SELECT \"_LastModified\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K'");
        Assert.True(after > before);
    }

    [Fact]
    public void TheOldScope_CanNoLongerTombstoneAnAdoptedRow()
    {
        snapshot.Merge([("K", "alpha", 1)], scope: "A");
        snapshot.Merge([("K", "alpha", 1)], scope: "B");

        // Scope A's next full pull is empty — its universe genuinely has nothing. The
        // adopted row belongs to B now and must survive.
        var emptyA = snapshot.Merge([], scope: "A");

        Assert.True(emptyA.Succeeded);
        Assert.Equal(0, emptyA.RowsTombstoned);
        Assert.Equal(1L, snapshot.Scalar<long>(
            "SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
    }

    [Fact]
    public void WithinOneScope_NothingCountsAsRescoped()
    {
        snapshot.Merge([("K", "alpha", 1)], scope: "A");
        var second = snapshot.Merge([("K", "alpha", 2)], scope: "A");

        Assert.Equal(1, second.RowsUpdated);
        Assert.Equal(0, second.RowsRescoped);
    }

    [Fact]
    public void NullScope_AndNamedScope_AreDistinctForAdoption()
    {
        snapshot.Merge([("K", "alpha", 1)]);   // scope null
        var adoption = snapshot.Merge([("K", "alpha", 1)], scope: "A");

        Assert.Equal(1, adoption.RowsRescoped);
        Assert.Equal("A", snapshot.Scalar<string>(
            "SELECT \"_SourceScope\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K'"));
    }

    [Fact]
    public void AdoptingATombstonedRow_CountsAsResurrection_NotRescope()
    {
        snapshot.Merge([("K", "alpha", 1)], scope: "A");
        snapshot.Merge([], scope: "A", force: true);   // wipe scope A intentionally

        var resurrection = snapshot.Merge([("K", "alpha", 1)], scope: "B");

        Assert.Equal(1, resurrection.RowsUpdated);
        Assert.Equal(0, resurrection.RowsRescoped);    // rescope counts LIVE rows only
        Assert.Equal("B", snapshot.Scalar<string>(
            "SELECT \"_SourceScope\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'K'"));
    }
}
