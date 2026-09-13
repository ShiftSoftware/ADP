using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Reads metrics from Cosmos DB. Scalar measures should select <c>... AS v</c>; grouped measures
/// should select <c>... AS k, ... AS v</c>. Null client => measures return Skipped-style errors so
/// a missing Cosmos connection string does not abort the whole run.
/// </summary>
public sealed class CosmosCheckSource(CosmosClient? client) : ICheckSource, ICheckSourceCatalog
{
    public string Name => "cosmos";

    public async Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct)
    {
        if (client is null)
            return new MeasureOutcome { Error = "Cosmos is not configured (ConnectionStrings:CosmosDB missing)." };

        if (string.IsNullOrWhiteSpace(spec.Sql) || string.IsNullOrWhiteSpace(spec.Database) || string.IsNullOrWhiteSpace(spec.Container))
            return new MeasureOutcome { Error = "cosmos measure requires 'sql', 'database' and 'container'." };

        try
        {
            var container = client.GetContainer(spec.Database, spec.Container);
            var cells = new Dictionary<string, MeasureCell>();

            using var iterator = container.GetItemQueryIterator<JObject>(new QueryDefinition(spec.Sql));
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct);
                foreach (var row in page)
                {
                    var key = grouped ? row["k"]?.ToString() ?? MeasureOutcome.ScalarKey : MeasureOutcome.ScalarKey;
                    var v = row["v"];
                    double? number = v is null || v.Type == JTokenType.Null ? null : v.Value<double?>();
                    cells[key] = new MeasureCell(number, null);
                }
            }

            return new MeasureOutcome { Cells = cells, AsOfUtc = DateTimeOffset.UtcNow };
        }
        catch (Exception ex)
        {
            return new MeasureOutcome { Error = ex.Message };
        }
    }

    public async Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct)
    {
        if (client is null)
            return new(Name, "Cosmos DB", [], DateTimeOffset.UtcNow, Error: "Cosmos metadata is unavailable because this source is not configured.");

        var datasets = new List<CatalogDataset>();
        using var databases = client.GetDatabaseQueryIterator<DatabaseProperties>();
        while (databases.HasMoreResults && datasets.Count < request.MaxDatasets)
        {
            var databasePage = await databases.ReadNextAsync(ct);
            foreach (var database in databasePage)
            {
                datasets.Add(new(database.Id, "database", null, []));
                if (datasets.Count >= request.MaxDatasets) break;
                using var containers = client.GetDatabase(database.Id).GetContainerQueryIterator<ContainerProperties>();
                while (containers.HasMoreResults && datasets.Count < request.MaxDatasets)
                {
                    var containerPage = await containers.ReadNextAsync(ct);
                    foreach (var container in containerPage)
                    {
                        datasets.Add(new(
                            container.Id,
                            "container",
                            database.Id,
                            [],
                            "Schema-less container; fields are intentionally not inferred from documents."));
                        if (datasets.Count >= request.MaxDatasets) break;
                    }
                }
                if (datasets.Count >= request.MaxDatasets) break;
            }
        }

        return new(
            Name,
            "Cosmos DB",
            datasets,
            DateTimeOffset.UtcNow,
            Truncated: datasets.Count >= request.MaxDatasets,
            Note: "Databases and containers only. No documents, field values, or sample records are read.");
    }
}
