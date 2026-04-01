using System.Collections.Generic;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Part;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// The aggregate model for part data from Cosmos DB, containing stock parts, catalog parts, and dead stock parts for a given part number.
/// </summary>
[Docable]
public class PartAggregateCosmosModel
{
    /// <summary>Stock parts across all locations/warehouses.</summary>
    public IEnumerable<StockPartModel> StockParts { get; set; }
    /// <summary>Catalog part records (typically one per part number).</summary>
    public IEnumerable<CatalogPartModel> CatalogParts { get; set; }
    /// <summary>Dead stock records by company/branch.</summary>
    public IEnumerable<CompanyDeadStockPartModel> CompanyDeadStockParts { get; set; }
}