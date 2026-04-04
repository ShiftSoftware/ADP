using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The storage backend used for vehicle lookup data.
/// </summary>
[Docable]
public enum StorageSources
{
    /// <summary>Azure Cosmos DB — used for real-time lookups.</summary>
    CosmosDB = 1,
    /// <summary>DuckDB — used for analytics and offline lookups.</summary>
    DuckDB = 2
}