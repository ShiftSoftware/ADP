using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// Placeholder for the menu DuckDB sync — the service that will populate the menu tables the DuckDB
/// reader queries (<see cref="DuckDBServiceMenuLookupStorageService"/>, layout contract in
/// <see cref="DuckDBServiceMenuSchema"/>).
///
/// <para><b>Not implemented yet, deliberately.</b> Where the sync pulls from and how it runs is its
/// own design decision, made separately — it is NOT simply a copy of the <c>ServiceMenus</c> Cosmos
/// container. This type only reserves the seam: the reader's table contract is fixed and tested, and
/// whatever this becomes must produce that layout. Until then, calling it throws — the same
/// not-implemented idiom as the Cosmos storage's bulk read.</para>
/// </summary>
public class DuckDBServiceMenuSyncService
{
    /// <exception cref="NotImplementedException">Always — the menu DuckDB sync is not implemented yet.</exception>
    public Task SyncAsync(
        global::DuckDB.NET.Data.DuckDBConnection connection,
        bool fullReload = false,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "The menu DuckDB sync is not implemented yet. The reader's table layout is defined by " +
            "DuckDBServiceMenuSchema; a future sync implementation populates those tables.");
}
