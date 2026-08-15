using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// Reads one basic model code's menu graph out of the <c>ServiceMenus</c> container.
///
/// <para><b>One query, one partition, no second round trip.</b> That is the entire point of the
/// container design (ADP.Menus/COSMOS_REPLICATION_PLAN.md §16): the documents are fully denormalized —
/// interval codes, group labour codes and membership, replacement-item operation codes, labour-rate
/// codes and brand abbreviations are all embedded — so nothing outside the partition is needed to
/// generate a line. There is deliberately no reference cache here, and adding one would reintroduce
/// exactly the staleness window §16 removed.</para>
///
/// <para>The container's partition key is hierarchical (<c>/BasicModelCode</c> then <c>/ItemType</c>),
/// so supplying only the first level is a PREFIX read: all four document types come back from the one
/// physical partition in a single request.</para>
/// </summary>
public class ServiceMenuCosmosService : IServiceMenuLookupStorageService
{
    private readonly LookUpCosmosClient client;

    public ServiceMenuCosmosService(LookUpCosmosClient client)
    {
        this.client = client;
    }

    private Container ServiceMenus => client.GetContainer(
        NoSQLConstants.Databases.Services,
        NoSQLConstants.Containers.ServiceMenus);

    /// <summary>
    /// Every document in the model's partition, split by item type. An empty result means the model has
    /// no replicated menu — not an error.
    /// </summary>
    /// <exception cref="ServiceMenuContainerNotFoundException">
    /// The database or container does not exist. Deliberately NOT swallowed into an empty result: a
    /// host that has never provisioned would otherwise be told "this model has no menu" for every model
    /// forever, with nothing anywhere to indicate why.
    /// </exception>
    /// <remarks>
    /// Virtual as a deliberate test seam. It is the one place the read path touches Cosmos, so overriding it
    /// lets everything above — the aggregator, the shared generator, the pricing and the vehicle-lookup
    /// section — run for real against fixture documents, offline. A fake one layer higher would test the
    /// wiring and skip the codes, which are the part worth testing.
    /// </remarks>
    public virtual async Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
    {
        var documents = new ServiceMenuDocuments { BasicModelCode = basicModelCode };

        if (string.IsNullOrWhiteSpace(basicModelCode))
            return documents;

        var code = basicModelCode.Trim();
        documents.BasicModelCode = code;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.BasicModelCode = @basicModelCode")
            .WithParameter("@basicModelCode", code);

        // Level 1 only — a prefix of the hierarchical key, which keeps the read single-partition while
        // returning every ItemType under it.
        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKeyBuilder().Add(code).Build(),
        };

        var container = ServiceMenus;
        var items = new List<JObject>();

        try
        {
            var iterator = container.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ServiceMenuContainerNotFoundException(container.Database.Id, container.Id, ex);
        }

        // Materialization faults (a stored document that no longer fits the model) wrap into the menu
        // subsystem's own storage exception: they are storage faults, and leaving them as raw
        // serializer exceptions would make them the ONE storage failure the vehicle lookup's menu
        // section does not contain — the same corrupt document would degrade a DuckDB-backed lookup
        // and fail a Cosmos-backed one outright.
        try
        {
            documents.Variants = Select<MenuVariantCosmosModel>(items, ModelTypes.MenuVariant);
            documents.Periods = Select<MenuPeriodCosmosModel>(items, ModelTypes.MenuPeriod);
            documents.Labours = Select<MenuLabourCosmosModel>(items, ModelTypes.MenuLabour);
            documents.Items = Select<MenuItemCosmosModel>(items, ModelTypes.MenuItem);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            throw new ServiceMenuStorageException(
                $"A stored menu document for basic model code '{code}' could not be materialized onto " +
                "its model — the document does not fit the expected shape.",
                ex);
        }

        return documents;
    }

    /// <summary>
    /// Deliberately not implemented, mirroring <see cref="CosmosVehicleLookupStorageService"/>'s bulk
    /// method: bulk lookups are a DuckDB-storage flow. On Cosmos a bulk read would just be N partition
    /// reads a caller can already make one at a time — the backend that makes bulk genuinely cheaper
    /// (a handful of <c>IN</c>-clause queries over a local file) is the one that implements it.
    /// </summary>
    public Task<IReadOnlyList<ServiceMenuDocuments>> GetMenuDocumentsAsync(IEnumerable<string> basicModelCodes, CancellationToken cancellationToken = default)
        => throw new System.NotImplementedException("Bulk menu document reads are implemented only for DuckDB storage.");

    /// <summary>
    /// Distinct trimmed codes in first-appearance order — the bulk contract's result order. Public and
    /// static because every <see cref="IServiceMenuLookupStorageService"/> implementation (this one,
    /// the DuckDB package's, a host's own) must normalize identically or the bulk contract's ordering
    /// silently differs per backend.
    /// </summary>
    public static List<string> NormalizeCodes(IEnumerable<string> basicModelCodes)
    {
        var codes = new List<string>();

        if (basicModelCodes is null)
            return codes;

        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var basicModelCode in basicModelCodes)
        {
            if (string.IsNullOrWhiteSpace(basicModelCode))
                continue;

            var code = basicModelCode.Trim();

            if (seen.Add(code))
                codes.Add(code);
        }

        return codes;
    }

    /// <summary>
    /// Projects the documents of one item type.
    ///
    /// Nothing is filtered on <c>IsDeleted</c> here, on purpose: which soft-deleted rows actually drop
    /// out of a menu is a generation-parity rule and it lives in
    /// <see cref="CosmosToGenerationAggregator"/>, where the export's own rules are mirrored and
    /// tested. Filtering at the read would quietly give the lookup different menu codes from the export
    /// — it is not the harmless-looking safety net it appears to be.
    /// </summary>
    private static List<T> Select<T>(List<JObject> items, string itemType) where T : class =>
        items
            .Where(document => (string)document[nameof(IPartitionedItem.ItemType)] == itemType)
            .Select(document => document.ToObject<T>())
            .Where(document => document is not null)
            .ToList();
}
