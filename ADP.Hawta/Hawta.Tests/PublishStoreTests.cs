using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The publish tier's storage contract. These tests pin the behaviours the blob port turns on —
/// the ones where <see cref="File"/>/<see cref="Directory"/> and blob disagree, and where the
/// disagreement is silent rather than loud.
///
/// <para>The blob implementation's own IO is proven against a real container by
/// <c>Snapshot.LocalRun --blob-probe</c> rather than here, deliberately: this suite must run on any
/// machine and in CI with no credential. What IS testable without a network is the part that
/// would leak a secret, and it is covered below.</para>
/// </summary>
public sealed class PublishStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"hawta-store-{Guid.NewGuid():N}");

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private LocalPublishStore Store()
    {
        var store = new LocalPublishStore(root);
        store.EnsureReady();
        return store;
    }

    // ---------------------------------------------------------------------------------------
    // The atomic commit. This is the guarantee the entire consumer contract rests on: a manifest
    // appears under its final name in exactly ONE operation, so a reader sees the previous complete
    // set or the new one and never a half-published state.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TryCommitNew_RefusesASecondWriter_AndLeavesTheIncumbentContentIntact()
    {
        var store = Store();
        var location = store.Resolve("company-data-read-20260815120000000.json");

        Assert.True(store.TryCommitNew(location, """{"commit":1}"""));

        // The refusal IS the guarantee. A caller that "helpfully" fell back to WriteAllText here
        // would silently destroy the property this method exists to provide.
        Assert.False(store.TryCommitNew(location, """{"commit":2}"""));
        Assert.Equal("""{"commit":1}""", store.ReadAllText(location));
    }

    [Fact]
    public void TryCommitNew_LeavesNoStagingResidue_WhenItLoses()
    {
        var store = Store();
        var location = store.Resolve("manifest.json");

        store.TryCommitNew(location, "first");
        store.TryCommitNew(location, "second");

        // A losing commit that abandoned its staging file would accumulate one per publish cadence,
        // and retention does not recognise the .staging shape, so nothing would ever collect them.
        Assert.DoesNotContain(store.List(), entry => entry.RelativePath.EndsWith(".staging", StringComparison.Ordinal));
    }

    [Fact]
    public void TryCommitNew_CreatesMissingParentFolders()
    {
        var store = Store();
        Assert.True(store.TryCommitNew(store.Resolve("Widget/20260815120000000.parquet.json"), "x"));
        Assert.True(store.Exists(store.Resolve("Widget/20260815120000000.parquet.json")));
    }

    // ---------------------------------------------------------------------------------------
    // Listing
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void List_IsRecursive_AndReportsForwardSlashedRelativePaths()
    {
        var store = Store();
        store.WriteAllText(store.Resolve("top.json"), "a");
        store.WriteAllText(store.Resolve("Widget/nested.parquet"), "b");

        var relative = store.List().Select(entry => entry.RelativePath).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        // Forward slashes regardless of platform: a manifest is read by consumers that are not
        // necessarily on Windows, and blob names have no other separator.
        Assert.Equal(["Widget/nested.parquet", "top.json"], relative);
    }

    [Fact]
    public void List_CarriesLengthAndLastWrite_BecauseRetentionsFloorIsMeasuredFromThem()
    {
        var store = Store();
        store.WriteAllText(store.Resolve("sized.json"), "1234567890");

        var entry = Assert.Single(store.List());
        Assert.Equal(10, entry.Length);
        Assert.True(entry.LastWriteUtc > DateTime.UtcNow.AddMinutes(-5), "last-write should be recent");
    }

    [Fact]
    public void List_OfAnAbsentSubtree_IsEmptyRatherThanThrowing()
    {
        var store = Store();
        Assert.Empty(store.List("NoSuchTable"));
    }

    [Fact]
    public void List_NarrowsToTheRequestedSubtree()
    {
        var store = Store();
        store.WriteAllText(store.Resolve("Widget/a.parquet"), "a");
        store.WriteAllText(store.Resolve("Gadget/b.parquet"), "b");

        var widget = store.List("Widget");
        Assert.Single(widget);
        Assert.Equal("Widget/a.parquet", Assert.Single(widget).RelativePath);
    }

    // ---------------------------------------------------------------------------------------
    // Absence must be quiet, and unreachability must not be
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Delete_OfSomethingAbsent_IsFalseRatherThanAThrow()
    {
        var store = Store();
        Assert.False(store.Delete(store.Resolve("never-existed.json")));
    }

    [Fact]
    public void LastWriteUtc_OfSomethingAbsent_IsNull()
    {
        var store = Store();
        Assert.Null(store.LastWriteUtc(store.Resolve("never-existed.json")));
    }

    [Fact]
    public void EnsureReady_CreatesTheDirectory_SoAFirstEverPublishHasSomewhereToLand()
    {
        Assert.False(Directory.Exists(root));
        new LocalPublishStore(root).EnsureReady();
        Assert.True(Directory.Exists(root));
    }

    // ---------------------------------------------------------------------------------------
    // Blob, without a network
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ABlobStoresRoot_NeverCarriesTheCredential()
    {
        // Root reaches manifests and log lines, so a SAS in it would be written into a published
        // artifact and printed into the startup line an operator pastes into a ticket.
        const string sas = "BlobEndpoint=https://acct.blob.core.windows.net;SharedAccessSignature=sv=2023-01-03&sig=SECRETVALUE%3D";
        var store = new BlobPublishStore(sas, "development", "powerbi/company-data-read");

        Assert.Equal("az://development/powerbi/company-data-read", store.Root);
        Assert.DoesNotContain("sig", store.Root, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SharedAccessSignature", store.Root, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRETVALUE", store.Root, StringComparison.Ordinal);
    }

    [Fact]
    public void ABlobStoreResolvesRelativePathsWithForwardSlashes_NotTheWindowsSeparator()
    {
        var store = new BlobPublishStore("UseDevelopmentStorage=true", "development", "snapshots");

        // Path.Combine would produce 'az://development/snapshots\Widget/x.parquet' on Windows — a
        // backslash inside a URI, silently, which is the whole reason PublishPath exists.
        Assert.Equal("az://development/snapshots/Widget/x.parquet", store.Resolve("Widget/x.parquet"));
    }

    [Fact]
    public void ABlobStoreWithNoPrefix_RootsAtTheContainer()
    {
        var store = new BlobPublishStore("UseDevelopmentStorage=true", "development");
        Assert.Equal("az://development", store.Root);
        Assert.Equal("az://development/latest.json", store.Resolve("latest.json"));
    }

    [Fact]
    public void ABlobStoreTrimsSlashesFromTheConfiguredPrefix()
    {
        // Operators write prefixes both ways; the difference must not produce two different roots.
        Assert.Equal(
            new BlobPublishStore("UseDevelopmentStorage=true", "development", "snapshots").Root,
            new BlobPublishStore("UseDevelopmentStorage=true", "development", "/snapshots/").Root);
    }
}
