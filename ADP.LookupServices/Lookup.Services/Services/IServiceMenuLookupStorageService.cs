using ShiftSoftware.ADP.Menus.Generation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// The storage abstraction for service-menu lookup data — the menu counterpart of
/// <see cref="IVehicleLookupStorageService"/>. Implemented by <see cref="ServiceMenuCosmosService"/>
/// (real-time, the replicated <c>ServiceMenus</c> container) and by the DuckDB reader in
/// <c>ShiftSoftware.ADP.Lookup.Services.DuckDB</c> (an offline snapshot of the same documents).
///
/// <para>The contract is one shape in both directions: a basic model code goes in, the model's ENTIRE
/// menu graph comes back as one <see cref="ServiceMenuDocuments"/> — all four document types, exactly
/// as stored, soft-delete flags and all. Implementations fetch; every inclusion rule stays in the
/// generation aggregator so the lookup cannot disagree with the DMS export no matter which backend
/// served the documents.</para>
/// </summary>
public interface IServiceMenuLookupStorageService
{
    /// <summary>
    /// Every stored menu document for one basic model code, split by item type. An empty result means
    /// the model has no replicated menu — an ordinary answer, not an error.
    /// </summary>
    /// <exception cref="ServiceMenuContainerNotFoundException">
    /// The backing store was never provisioned (the Cosmos container, or the DuckDB menu tables, do not
    /// exist). Deliberately not folded into an empty result — see the exception's remarks.
    /// </exception>
    /// <exception cref="ServiceMenuStorageException">
    /// The store exists but its content could not be read or materialized onto the models. Cosmos
    /// implementations still let the SDK's <c>CosmosException</c> surface for transport faults;
    /// callers that contain storage faults handle both.
    /// </exception>
    Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// The bulk read: menu documents for many basic model codes in as few round trips as the backend
    /// allows. Returns one <see cref="ServiceMenuDocuments"/> per DISTINCT code (after trimming), in
    /// first-appearance order; a code with no documents yields an empty entry rather than being
    /// dropped, so callers can zip results back to their input.
    ///
    /// <para><b>A DuckDB-storage flow</b>, exactly like
    /// <see cref="IVehicleLookupStorageService.GetAggregatedCompanyDataForBulkLookupAsync"/>: the
    /// Cosmos implementation throws <see cref="System.NotImplementedException"/>, because on Cosmos a
    /// bulk read would only be N partition reads a caller can already make one at a time.</para>
    /// </summary>
    /// <exception cref="ServiceMenuContainerNotFoundException">As for the single-code read.</exception>
    /// <exception cref="ServiceMenuStorageException">As for the single-code read.</exception>
    /// <exception cref="System.NotImplementedException">The backend does not support bulk reads (Cosmos).</exception>
    Task<IReadOnlyList<ServiceMenuDocuments>> GetMenuDocumentsAsync(IEnumerable<string> basicModelCodes, CancellationToken cancellationToken = default);
}
