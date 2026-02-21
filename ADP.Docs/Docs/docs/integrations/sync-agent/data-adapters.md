# Data Adapters

Data Adapters are the building blocks of every ADP Sync Agent pipeline. They encapsulate the logic for reading from a data source or writing to a data destination, and they automatically wire themselves into the Sync Engine lifecycle.

The Sync Agent ships with built-in adapters for the most common enterprise data systems. When a built-in adapter doesn't fit, you can implement the `ISyncDataAdapter` interface to create your own.

## Source Adapters

Source adapters are responsible for reading data from the origin system and feeding it to the Sync Engine in batches.

---

### CSV Source Adapters

The CSV source adapters are designed for file-based data integration — a common scenario when dealer management systems export data as flat files.

Rather than naively re-processing the entire file on every run, the CSV adapters use a **git-based diff strategy**:

1. **Load the previously synced version** of the CSV file from a content repository.
2. **Load the new version** from the source location.
3. **Compare the two versions** using a git diff to identify only the added and deleted lines.
4. **Feed only the changes** into the pipeline as Add and Delete actions.

This approach dramatically reduces processing volume and ensures that only actual changes are synced.

!!! info "Reordered Lines"
    CSV files from external systems may contain the same data in a different order between exports. The `SkipReorderedLines` option detects lines that appear in both the added and deleted sets (i.e., they moved but didn't change) and excludes them from processing.

The Sync Agent provides two CSV adapter implementations:

#### CsvHelperCsvSyncDataSource

Uses the [CsvHelper](https://joshclose.github.io/CsvHelper/) library for CSV parsing. This is the recommended adapter for most scenarios — it supports header-based mapping, flexible configuration, and culture-aware parsing.

```csharp
// Register in DI
services.AddCSVSyncDataSource<FileSystemStorageService>(options =>
{
    options.CompareWorkingDirectory = @"C:\temp\csv-compare";
    options.SourceBasePath = @"C:\data\incoming";
    options.DestinationBasePath = @"C:\data\synced";
});
```

#### FileHelperCsvSyncDataSource

Uses the [FileHelpers](https://www.filehelpers.net/) library for CSV parsing. This adapter requires your CSV model class to inherit from `CacheableCSV`, which provides an auto-computed `id` field (SHA512 hash of the raw CSV line).

```csharp
public class PartRecord : CacheableCSV
{
    public string PartNumber { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
```

#### CSV Source Configuration

Both CSV adapters are configured with `CSVSyncDataSourceConfigurations<T>`:

| Property | Type | Description |
|---|---|---|
| `CSVFileName` | `string` | **Required.** Name of the CSV file to sync. |
| `SourceContainerOrShareName` | `string?` | Container or file share name for the source (Azure Storage). |
| `SourceDirectory` | `string?` | Directory path within the source container. |
| `DestinationContainerOrShareName` | `string?` | Container or file share name for the synced copy. |
| `DestinationDirectory` | `string?` | Directory path within the destination container. |
| `SkipReorderedLines` | `bool` | When `true`, lines that appear in both added and deleted sets are excluded. |
| `HasHeaderRecord` | `bool` | Whether the CSV file has a header row. Default: `true`. |
| `ProccessSourceData` | `Func<...>?` | Optional function to pre-process source data before comparison. |
| `ProccessAddedItems` | `Func<...>?` | Optional function to post-process added items after diffing. |
| `ProccessDeletedItems` | `Func<...>?` | Optional function to post-process deleted items after diffing. |

#### Storage Backends

The CSV adapters rely on an `IStorageService` to load and store CSV files. The built-in `FileSystemStorageService` works with local or network file paths. For Azure Blob Storage or Azure File Share, implement a custom `IStorageService`.

---

### EF Core Source Adapter

The `EFCoreSyncDataSource` reads data from a SQL Server database (or any EF Core-compatible provider) and feeds it to the pipeline in batches.

Key features:

- **Automatic batching** by primary key with ordered, keyset-based pagination.
- **Sync timestamp tracking** — after a successful batch, updates a `LastSynced` or equivalent timestamp column on processed records to avoid re-syncing.
- **Custom queries** — filter and project data using LINQ before it enters the pipeline.

```csharp
var source = engine.SetDataAddapter<EFCoreSyncDataSource<PartEntity, AppDbContext>>(services);

source.Configure(new EFCoreSyncDataSourceConfigurations<PartEntity>
{
    Query = (query, actionType) => query.Where(p => p.LastModified > lastSync),
    EntityKey = p => p.Id,
    SyncTimestamp = p => p.LastSyncedAt,
});
```

#### EF Core Source Configuration

`EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>`:

| Property | Type | Description |
|---|---|---|
| `Query` | `Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>>` | **Required.** LINQ query to filter and project source data. Receives the action type for conditional filtering. |
| `EntityKey` | `Expression<Func<TEntity, object>>?` | Primary key expression used for keyset-based batching. |
| `SourceKey` | `Expression<Func<TSource, object>>?` | Key expression on the source projection type. |
| `DestinationKey` | `Expression<Func<TDestination, object>>?` | Key expression on the destination type — used to match items when updating sync timestamps. |
| `SyncTimestamp` | `Expression<Func<TEntity, DateTimeOffset?>>?` | Property to update with the sync timestamp after each successful batch. |
| `UpdateSyncTimeStampForSkippedItems` | `bool` | If `true`, also updates the sync timestamp for items that were skipped during storage. Default: `false`. |
| `UpdateTimeStampFilter` | `Func<...>?` | Custom filter for selecting which entity records to update with the sync timestamp. |

#### Generic Overloads

The EF Core source adapter comes in three generic overloads to handle different scenarios:

| Overload | Use Case |
|---|---|
| `EFCoreSyncDataSource<T, TDbContext>` | Entity, source, and destination are the same type. |
| `EFCoreSyncDataSource<TSource, TDestination, TDbContext>` | Entity and source are the same; destination differs. |
| `EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext>` | All three types are different (e.g., entity → DTO → Cosmos document). |

---

## Destination Adapters

Destination adapters are responsible for writing transformed data to the target system.

---

### Cosmos DB Destination Adapter

The `CosmosSyncDataDestination` writes data to Azure Cosmos DB containers. It treats Add and Update actions as **upsert** operations and supports Delete actions natively.

Key features:

- **Parallel upserts** with concurrency control.
- **Built-in Polly retry pipeline** for transient Cosmos DB failures.
- **Partition key support** — up to 3 levels of hierarchical partition keys via expressions.
- **Patch operations** — optionally use Cosmos DB partial updates instead of full document replacement.
- **Custom Cosmos actions** — override the default upsert/delete behavior per item (e.g., change a delete to an upsert with a soft-delete flag).

```csharp
var destination = engine.SetDataAddapter<CosmosSyncDataDestination<PartDoc, CosmosClient>>(services);

destination.Configure(new CosmosSyncDataDestinationConfigurations<PartDoc>
{
    DatabaseId = "PartsDB",
    ContainerId = "Parts",
    PartitionKeyLevel1Expression = x => x.Region,
});
```

#### Cosmos DB Destination Configuration

`CosmosSyncDataDestinationConfigurations<TDestination, TCosmos>`:

| Property | Type | Description |
|---|---|---|
| `DatabaseId` | `string` | **Required.** Cosmos DB database ID. |
| `ContainerId` | `string` | **Required.** Cosmos DB container ID. |
| `PartitionKeyLevel1Expression` | `Expression<...>` | **Required.** Expression for the first level of the partition key. |
| `PartitionKeyLevel2Expression` | `Expression<...>?` | Second level of a hierarchical partition key. |
| `PartitionKeyLevel3Expression` | `Expression<...>?` | Third level of a hierarchical partition key. |
| `CosmosAction` | `Func<...>?` | Custom function to override the default action (upsert/delete) per item. |
| `UsePatch` | `bool` | Use Cosmos DB patch (partial update) instead of full document replace. |
| `PropertiesToPatch` | `Expression<...>[]?` | Specific properties to patch. If empty and `UsePatch` is `true`, all properties are patched. |

---

### EF Core Destination Adapter

The `EFCoreSyncDataDestination` writes data to a SQL Server database (or any EF Core-compatible provider) using bulk operations powered by [EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions).

Key features:

- **Bulk insert or update** — uses `BulkInsertOrUpdateAsync` for high-performance writes.
- **Configurable BulkConfig** — full access to the underlying BulkExtensions configuration.

```csharp
var destination = engine.SetDataAddapter<EFCoreSyncDataDestination<PartEntity, AppDbContext>>(services);

destination.Configure(new EFCoreSyncDataDestinationConfigurations
{
    BulkConfig = new BulkConfig { SetOutputIdentity = true }
});
```

#### EF Core Destination Configuration

`EFCoreSyncDataDestinationConfigurations`:

| Property | Type | Description |
|---|---|---|
| `BulkConfig` | `BulkConfig` | Configuration for EFCore.BulkExtensions (batch size, identity handling, etc.). |

---

### DuckDB Destination Adapter

The `DuckDBSyncDataDestination` writes data to a [DuckDB](https://duckdb.org/) database — an in-process analytical database that's ideal for reporting, analytics, and data warehousing scenarios.

Key features:

- **Automatic table creation** from the destination model's properties with correct DuckDB type mapping.
- **Staging table pattern** — data is first bulk-loaded into a temporary staging table, then upserted into the main table for atomicity.
- **Primary key support** — configurable primary key for upsert behavior.
- **Complex type handling** — nested objects are stored as JSON columns.

```csharp
var destination = engine.SetDataAddapter<DuckDBSyncDataDestination<SourceModel, DestModel, DuckDBConnection>>(services);

destination.Configure(new DuckDBSyncDataDestinationConfigurations<SourceModel, DestModel>
{
    TableName = "Parts",
    PrimaryKey = x => x.PartNumber,
    ContinueAfterFail = true,
});
```

#### DuckDB Destination Configuration

`DuckDBSyncDataDestinationConfigurations<TSource, TDestination>`:

| Property | Type | Description |
|---|---|---|
| `TableName` | `string` | **Required.** Name of the DuckDB table. |
| `PrimaryKey` | `Expression<...>?` | Expression for the primary key column (enables upsert). |
| `ContinueAfterFail` | `bool` | If `true`, continue processing remaining items after a row fails. Default: `false`. |

#### DuckDB Type Mapping

The adapter automatically maps C# types to DuckDB column types:

| C# Type | DuckDB Type |
|---|---|
| `bool` | `BOOLEAN` |
| `int` | `INTEGER` |
| `long` | `BIGINT` |
| `double` | `DOUBLE` |
| `decimal` | `DECIMAL(38, 10)` |
| `string` | `VARCHAR` |
| `DateTime` | `TIMESTAMP` |
| `DateTimeOffset` | `TIMESTAMPTZ` |
| `Guid` | `UUID` |
| `enum` | `INTEGER` |
| Complex types | `JSON` |

---

## Custom Adapters

When the built-in adapters don't cover your use case, you can implement the `ISyncDataAdapter` interface to create a custom adapter for any data system.

A custom adapter must implement:

- `SetSyncService` — Store a reference to the Sync Engine.
- `Configure` — Wire up the adapter's logic into the engine's lifecycle stages.
- The relevant lifecycle methods (`Preparing`, `GetSourceBatchItems`, `StoreBatchData`, etc.).

Methods that are not relevant to your adapter can throw `NotImplementedException` — only the methods you wire into the engine via `Configure` will be called.

```csharp
public class MyCustomSource<TSource, TDestination> 
    : ISyncDataAdapter<TSource, TDestination, MyConfig, MyCustomSource<TSource, TDestination>>
    where TSource : class
    where TDestination : class
{
    public ISyncEngine<TSource, TDestination> SyncService { get; private set; }
    public MyConfig? Configurations { get; private set; }

    public MyCustomSource<TSource, TDestination> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
    {
        SyncService = syncService;
        return this;
    }

    public ISyncEngine<TSource, TDestination> Configure(MyConfig configurations, bool configureSyncService = true)
    {
        Configurations = configurations;

        if (configureSyncService)
            SyncService
                .SetupGetSourceBatchItems(GetSourceBatchItems)
                .SetupSourceTotalItemCount(SourceTotalItemCount);

        return SyncService;
    }

    // Implement the methods you need...
}
```

See [Architecture](architecture.md) for details on each lifecycle stage and when it's called.
