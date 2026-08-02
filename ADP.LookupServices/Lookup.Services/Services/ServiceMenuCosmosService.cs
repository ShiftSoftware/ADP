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
public class ServiceMenuCosmosService
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
    public async Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
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

        documents.Variants = Select<MenuVariantCosmosModel>(items, ModelTypes.MenuVariant);
        documents.Periods = Select<MenuPeriodCosmosModel>(items, ModelTypes.MenuPeriod);
        documents.Labours = Select<MenuLabourCosmosModel>(items, ModelTypes.MenuLabour);
        documents.Items = Select<MenuItemCosmosModel>(items, ModelTypes.MenuItem);

        return documents;
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
