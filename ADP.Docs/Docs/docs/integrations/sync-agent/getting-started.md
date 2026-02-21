# Getting Started

This guide walks you through setting up the ADP Sync Agent with dependency injection and building your first data synchronization pipeline.

## Installation

Install the NuGet package:

```
dotnet add package ShiftSoftware.ADP.SyncAgent
```

## Service Registration

The Sync Agent provides extension methods on `IServiceCollection` to register its components:

```csharp
services.AddSyncService();                  // Registers SyncEngine<T> and SyncEngine<TSource, TDestination>
services.AddEFCoreSyncDataSource();         // Registers EFCoreSyncDataSource adapters
services.AddEFCoreSyncDataDestination();    // Registers EFCoreSyncDataDestination adapters
services.AddCosmosSyncDataDestination();    // Registers CosmosSyncDataDestination adapters

// For CSV-based pipelines
services.AddCSVSyncDataSource<FileSystemStorageService>(options =>
{
    options.CompareWorkingDirectory = @"C:\temp\csv-compare";
    options.SourceBasePath = @"C:\data\incoming";
    options.DestinationBasePath = @"C:\data\synced";
});
```

---

## Example 1: EF Core to Cosmos DB

A common scenario — reading from a SQL Server database and syncing to Azure Cosmos DB.

```csharp
public class PartSyncJob
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PartSyncJob> _logger;

    public PartSyncJob(IServiceProvider services, ILogger<PartSyncJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> RunAsync()
    {
        // 1. Create a Sync Engine
        await using var engine = new SyncEngine<PartEntity, PartCosmosDoc>(_services);

        // 2. Configure the engine
        engine.Configure(
            batchSize: 500,
            maxRetryCount: 3,
            operationTimeoutInSeconds: 600);

        // 3. Register a logger
        engine.RegisterLogger(new SyncEngineILogger(_logger));

        // 4. Attach the EF Core source adapter
        var source = engine
            .SetDataAddapter<EFCoreSyncDataSource<PartEntity, AppDbContext>>(_services);

        source.Configure(new EFCoreSyncDataSourceConfigurations<PartEntity>
        {
            Query = (query, actionType) => query
                .Where(p => p.LastSyncedAt == null || p.LastModified > p.LastSyncedAt),
            EntityKey = p => p.Id,
            SyncTimestamp = p => p.LastSyncedAt,
        });

        // 5. Attach the Cosmos DB destination adapter
        var destination = engine
            .SetDataAddapter<CosmosSyncDataDestination<PartEntity, PartCosmosDoc, CosmosClient>>(_services);

        destination.Configure(new CosmosSyncDataDestinationConfigurations<PartEntity, PartCosmosDoc>
        {
            DatabaseId = "PartsDB",
            ContainerId = "Parts",
            PartitionKeyLevel1Expression = x => x.Region,
        });

        // 6. Set up mapping
        engine.SetupMapping(async (items, actionType) =>
        {
            return items?.Select(p => p is null ? null : new PartCosmosDoc
            {
                id = p.Id.ToString(),
                PartNumber = p.PartNumber,
                Description = p.Description,
                Price = p.Price,
                Region = p.Region,
            });
        });

        // 7. Run the pipeline
        return await engine.RunAsync();
    }
}
```

---

## Example 2: CSV to Cosmos DB

Syncing data from CSV flat files to Azure Cosmos DB — common when integrating with dealer management systems that export data as files.

```csharp
public class PriceSyncJob
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PriceSyncJob> _logger;

    public PriceSyncJob(IServiceProvider services, ILogger<PriceSyncJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> RunAsync()
    {
        await using var engine = new SyncEngine<PriceRecord, PriceCosmosDoc>(_services);

        engine.Configure(
            actionExecutionAndOrder: [SyncActionType.Delete, SyncActionType.Add],
            batchSize: 1000,
            maxRetryCount: 2,
            operationTimeoutInSeconds: 900);

        engine.RegisterLogger(new SyncEngineILogger(_logger));

        // Attach CSV source (CsvHelper-based)
        var source = engine
            .SetDataAddapter<CsvHelperCsvSyncDataSource<PriceRecord, PriceCosmosDoc>>(_services);

        source.Configure(new CSVSyncDataSourceConfigurations<PriceRecord>
        {
            CSVFileName = "prices.csv",
            SourceDirectory = "dealer-exports",
            DestinationDirectory = "synced",
            SkipReorderedLines = true,
            HasHeaderRecord = true,
        });

        // Attach Cosmos DB destination
        var destination = engine
            .SetDataAddapter<CosmosSyncDataDestination<PriceRecord, PriceCosmosDoc, CosmosClient>>(_services);

        destination.Configure(new CosmosSyncDataDestinationConfigurations<PriceRecord, PriceCosmosDoc>
        {
            DatabaseId = "PricingDB",
            ContainerId = "Prices",
            PartitionKeyLevel1Expression = x => x.Region,
        });

        // Mapping
        engine.SetupMapping(async (items, actionType) =>
        {
            return items?.Select(p => p is null ? null : new PriceCosmosDoc
            {
                id = p.PartNumber,
                PartNumber = p.PartNumber,
                Price = p.Price,
                Currency = p.Currency,
                Region = p.Region,
            });
        });

        return await engine.RunAsync();
    }
}
```

---

## Example 3: EF Core to EF Core (Database to Database)

Syncing data between two SQL Server databases — useful for replicating data across environments or aggregating dealer data.

```csharp
public class OrderSyncJob
{
    private readonly IServiceProvider _services;

    public OrderSyncJob(IServiceProvider services) => _services = services;

    public async Task<bool> RunAsync()
    {
        await using var engine = new SyncEngine<OrderEntity>(_services);

        engine.Configure(batchSize: 200, maxRetryCount: 1);

        // Source: read from DealerDbContext
        var source = engine
            .SetDataAddapter<EFCoreSyncDataSource<OrderEntity, DealerDbContext>>(_services);

        source.Configure(new EFCoreSyncDataSourceConfigurations<OrderEntity>
        {
            Query = (query, actionType) => query.Where(o => o.Status == "Completed"),
            EntityKey = o => o.OrderId,
        });

        // Destination: write to CentralDbContext
        var destination = engine
            .SetDataAddapter<EFCoreSyncDataDestination<OrderEntity, CentralDbContext>>(_services);

        destination.Configure(new EFCoreSyncDataDestinationConfigurations());

        // Same type — use identity mapping
        engine.SetupMapping(async (items, actionType) => items);

        return await engine.RunAsync();
    }
}
```

---

## Example 4: EF Core to DuckDB

Syncing operational data into DuckDB for analytics and reporting.

```csharp
public class AnalyticsSyncJob
{
    private readonly IServiceProvider _services;

    public AnalyticsSyncJob(IServiceProvider services) => _services = services;

    public async Task<bool> RunAsync()
    {
        await using var engine = new SyncEngine<SalesEntity, SalesAnalyticsRow>(_services);

        engine.Configure(batchSize: 5000);

        var source = engine
            .SetDataAddapter<EFCoreSyncDataSource<SalesEntity, SalesAnalyticsRow, SalesDbContext>>(_services);

        source.Configure(new EFCoreSyncDataSourceConfigurations<SalesEntity, SalesAnalyticsRow>
        {
            Query = (query, actionType) => query
                .Select(s => new SalesAnalyticsRow
                {
                    SaleId = s.Id,
                    DealerId = s.DealerId,
                    Amount = s.TotalAmount,
                    SaleDate = s.CreatedAt,
                }),
            EntityKey = s => s.Id,
        });

        var destination = engine
            .SetDataAddapter<DuckDBSyncDataDestination<SalesEntity, SalesAnalyticsRow, DuckDBConnection>>(_services);

        destination.Configure(new DuckDBSyncDataDestinationConfigurations<SalesEntity, SalesAnalyticsRow>
        {
            TableName = "Sales",
            PrimaryKey = x => x.SaleId,
        });

        engine.SetupMapping(async (items, actionType) => items);

        return await engine.RunAsync();
    }
}
```

---

## Example 5: Using AutoMapper

The Sync Agent integrates with [AutoMapper](https://automapper.org/) via the `UseAutoMapper` extension method:

```csharp
engine.UseAutoMapper(mapper);
```

This replaces the need for a manual `SetupMapping` call. The mapper will be used to transform `TSource` items into `TDestination` items automatically.

---

## Pipeline Triggers

The Sync Agent does not dictate how pipelines are triggered. You can integrate it with any scheduling or event system:

| Trigger | Example |
|---|---|
| **Scheduled Jobs** | Hangfire, Azure Functions (Timer Trigger), Windows Services, cron jobs. |
| **Event-Based** | Azure Service Bus messages, Azure Blob Storage events, database change feeds. |
| **On-Demand** | API endpoints, manual UI triggers. |

---

## Tips

!!! tip "Source Before Destination"
    Always configure the **source adapter before the destination adapter**. The adapters chain their lifecycle hooks — the destination adapter may depend on hooks already set by the source.

!!! tip "Batch Size Tuning"
    Start with a moderate batch size (500–1000) and adjust based on your data volume and destination latency. Smaller batches give more granular retry but add overhead. Larger batches are more efficient but risk more data on failure.

!!! tip "Timeout Planning"
    Set `operationTimeoutInSeconds` generously for initial syncs or large datasets. For recurring syncs with small deltas, a shorter timeout helps detect stuck pipelines early.

!!! tip "Dispose the Engine"
    `SyncEngine` implements `IAsyncDisposable`. Use `await using` to ensure all resources (adapters, file handles, connections) are cleaned up after the pipeline completes.
