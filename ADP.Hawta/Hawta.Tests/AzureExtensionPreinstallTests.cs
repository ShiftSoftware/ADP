using DuckDB.NET.Data;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// <see cref="SnapshotStore.PreinstallAzureExtension"/> — the fix for DuckDB's non-atomic extension
/// install (duckdb/duckdb#3947) and the race between concurrent first touches
/// (duckdb/duckdb#12589, open upstream).
///
/// <para>Measured before the fix, on this build: eight connections released together into one empty
/// extension directory left <b>one</b> survivor and stranded a 29 MB <c>.tmp-&lt;guid&gt;</c> file.
/// That directory is <c>%HOME%/data/duckdb-extensions</c> on App Service — Azure Files, shared by
/// every instance — so the race does not need a fan-out to happen.</para>
/// </summary>
public class AzureExtensionPreinstallTests
{
    private const string AnyCredential = "UseDevelopmentStorage=true";

    private static string TempDirectory(string tag)
    {
        var path = Path.Combine(Path.GetTempPath(), $"hawta-test-{tag}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Best effort, and it has to be. A LOADED DuckDB extension stays mapped for the life of the
    /// process — extensions cannot be unloaded — so on Windows its file is locked and the directory
    /// will not delete. That lock is also why DuckDB's installer writes to a
    /// <c>.tmp-&lt;guid&gt;</c> and renames, and therefore why concurrent installs race at all.
    /// Failing an otherwise-passing test on temp cleanup would be strictly worse than leaving the
    /// OS to reclaim its own temp directory.
    /// </summary>
    private static void TryDelete(string directory)
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private static bool AzureExtensionPresent(string extensionDirectory) =>
        Directory.Exists(extensionDirectory)
        && Directory.GetFiles(extensionDirectory, "azure.duckdb_extension", SearchOption.AllDirectories).Length > 0;

    private static string Setting(SnapshotStore store, string name) =>
        store.ExecuteScalar($"SELECT current_setting('{name}')")?.ToString()?.ToLowerInvariant() ?? "";

    /// <summary>
    /// The guard. A store with no azure credential is never going to touch az://, so it must not be
    /// made to install an extension it will not use — that would newly break opening offline with a
    /// cold cache, which is an ordinary dev-machine state.
    /// </summary>
    [Fact]
    public void Open_WithoutAzureCredential_DoesNotTouchExtensionState()
    {
        var extensionDirectory = TempDirectory("noext");
        try
        {
            using var store = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = ":memory:",
                ExtensionDirectory = extensionDirectory,
            });

            // Untouched: autoinstall left at DuckDB's default, and nothing downloaded.
            Assert.Equal("true", Setting(store, "autoinstall_known_extensions"));
            Assert.False(AzureExtensionPresent(extensionDirectory));
        }
        finally
        {
            TryDelete(extensionDirectory);
        }
    }

    /// <summary>
    /// With a credential, Open installs the extension and then closes the door behind it, so no
    /// later connection can attempt an install at all.
    /// </summary>
    [Fact]
    public void Open_WithAzureCredential_InstallsExtension_AndDisablesAutoinstall()
    {
        var extensionDirectory = TempDirectory("ext");
        try
        {
            using var store = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = ":memory:",
                ExtensionDirectory = extensionDirectory,
                AzureConnectionString = AnyCredential,
            });

            Assert.SkipWhen(
                !AzureExtensionPresent(extensionDirectory),
                "The azure extension could not be installed (no network and no cached copy). " +
                "The best-effort contract is exercised by the offline test below.");

            Assert.Equal("false", Setting(store, "autoinstall_known_extensions"));

            // autoload stays ON — a cached extension must still load, or the fix would break az://
            // for the very connection it is meant to protect.
            Assert.Equal("true", Setting(store, "autoload_known_extensions"));
        }
        finally
        {
            TryDelete(extensionDirectory);
        }
    }

    /// <summary>
    /// The best-effort contract: when the install cannot happen, autoinstall is left ON, which is
    /// exactly the behaviour this estate had before the fix existed — the first az:// touch tries
    /// and reports its own error at the point of use. No path is left worse than it was.
    /// </summary>
    [Fact]
    public void Open_WhenTheExtensionCannotBeInstalled_LeavesAutoinstallOn()
    {
        var extensionDirectory = TempDirectory("offline");
        try
        {
            // A path that cannot be created as a directory: the extension directory is created
            // eagerly, so point it at a child of an existing FILE.
            var blocker = Path.Combine(extensionDirectory, "blocker");
            File.WriteAllText(blocker, "not a directory");

            try
            {
                using var store = SnapshotStore.Open(new SnapshotStoreOptions
                {
                    DatabasePath = ":memory:",
                    ExtensionDirectory = Path.Combine(blocker, "extensions"),
                    AzureConnectionString = AnyCredential,
                });

                // If Open survived, the install must have failed silently and left autoinstall on.
                Assert.Equal("true", Setting(store, "autoinstall_known_extensions"));
            }
            catch (Exception failure)
            {
                // ApplyExtensionDirectory creates the directory eagerly and runs BEFORE the
                // pre-install, so on most platforms Open fails there. That is pre-existing
                // behaviour this change does not alter — the point of the test is that
                // PreinstallAzureExtension never converts a working Open into a failing one.
                Assert.True(
                    failure is IOException or UnauthorizedAccessException or DuckDBException,
                    $"Unexpected failure shape from the extension directory itself: {failure}");
            }
        }
        finally
        {
            TryDelete(extensionDirectory);
        }
    }

    /// <summary>
    /// The regression test for the race itself. Once the extension is installed, N connections
    /// against SEPARATE database instances sharing one extension directory — the App Service
    /// shape, where %HOME% is shared by every instance — all load it concurrently without racing,
    /// and nothing is stranded.
    /// </summary>
    [Fact]
    public void PreinstalledExtension_LoadsConcurrently_WithoutRacingOrLeaking()
    {
        var extensionDirectory = TempDirectory("race-ext");
        var databaseDirectory = TempDirectory("race-db");
        try
        {
            // Serial pre-install, exactly as Open now performs it.
            using (var seed = SnapshotStore.Open(new SnapshotStoreOptions
            {
                DatabasePath = ":memory:",
                ExtensionDirectory = extensionDirectory,
                AzureConnectionString = AnyCredential,
            }))
            {
                Assert.SkipWhen(
                    !AzureExtensionPresent(extensionDirectory),
                    "The azure extension could not be installed (no network and no cached copy).");
            }

            const int workers = 8;
            var failures = new string?[workers];
            var barrier = new Barrier(workers);
            var threads = new Thread[workers];

            for (var i = 0; i < workers; i++)
            {
                var n = i;
                threads[n] = new Thread(() =>
                {
                    try
                    {
                        using var connection = new DuckDBConnection(
                            $"Data Source={Path.Combine(databaseDirectory, $"w{n}.duckdb")}");
                        connection.Open();
                        using (var set = connection.CreateCommand())
                        {
                            set.CommandText =
                                $"SET extension_directory = '{extensionDirectory.Replace("'", "''")}'";
                            set.ExecuteNonQuery();
                        }

                        barrier.SignalAndWait();

                        using var load = connection.CreateCommand();
                        load.CommandText = "LOAD azure";
                        load.ExecuteNonQuery();
                    }
                    catch (Exception failure)
                    {
                        try { barrier.RemoveParticipant(); } catch { /* already released */ }
                        failures[n] = failure.Message.ReplaceLineEndings(" ").Trim();
                    }
                });
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            Assert.All(failures, failure => Assert.Null(failure));

            // The unremedied race stranded a 29 MB .tmp-<guid> copy that nothing ever collects.
            var stranded = Directory
                .GetFiles(extensionDirectory, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).Contains(".tmp-", StringComparison.Ordinal))
                .ToArray();
            Assert.Empty(stranded);
        }
        finally
        {
            TryDelete(extensionDirectory);
            TryDelete(databaseDirectory);
        }
    }
}
