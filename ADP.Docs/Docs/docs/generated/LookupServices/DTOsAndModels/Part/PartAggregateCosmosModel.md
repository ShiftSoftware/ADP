---
hide:
    - toc
---
The aggregate model for part data from Cosmos DB, containing stock parts, catalog parts, and dead stock parts for a given part number.

| Property | Summary |
|----------|---------|
| StockParts <div><strong>``IEnumerable<StockPartModel>``</strong></div> | Stock parts across all locations/warehouses. |
| CatalogParts <div><strong>``IEnumerable<CatalogPartModel>``</strong></div> | Catalog part records (typically one per part number). |
| CompanyDeadStockParts <div><strong>``IEnumerable<CompanyDeadStockPartModel>``</strong></div> | Dead stock records by company/branch. |
