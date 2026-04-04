---
hide:
    - toc
---
The main result returned by the part lookup service.
 Contains part catalog information, pricing, stock availability, supersessions, HS codes, and dead stock data.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The unique part number. |
| PartDescription <div><strong>``string``</strong></div> | The part description from the catalog. |
| LocalDescription <div><strong>``string``</strong></div> | The localized description of the part. |
| ProductGroup <div><strong>``string``</strong></div> | The product group code the part belongs to. |
| PNC <div><strong>``string``</strong></div> | The Product Number Code (PNC). |
| BinType <div><strong>``string``</strong></div> | The bin type for storage classification. |
| ShowManufacturerPartLookup <div><strong>``bool``</strong></div> | Whether the manufacturer part lookup feature is available for this part. |
| DistributorPurchasePrice <div><strong>``decimal?``</strong></div> | The purchase price that the distributor pays for this part. |
| Length <div><strong>``decimal?``</strong></div> | The length of the part. |
| Width <div><strong>``decimal?``</strong></div> | The width of the part. |
| Height <div><strong>``decimal?``</strong></div> | The height of the part. |
| NetWeight <div><strong>``decimal?``</strong></div> | The net weight of the part. |
| GrossWeight <div><strong>``decimal?``</strong></div> | The gross weight of the part. |
| CubicMeasure <div><strong>``decimal?``</strong></div> | The cubic measure of the part. |
| HSCode <div><strong>``string``</strong></div> | The Harmonized System (HS) code for the part. |
| Origin <div><strong>``string``</strong></div> | The country of origin of the part. |
| SupersededTo <div><strong>``IEnumerable<string>``</strong></div> | Part numbers that this part has been superseded to (newer replacements). |
| SupersededFrom <div><strong>``IEnumerable<string>``</strong></div> | Part numbers that have been superseded by this part (older parts this replaces). |
| StockParts <div><strong>``IEnumerable<StockPartDTO>``</strong></div> | Stock availability across locations. |
| Prices <div><strong>``IEnumerable<PartPriceDTO>``</strong></div> | Pricing information by country and region. |
| HSCodes <div><strong>``IEnumerable<HSCodeDTO>``</strong></div> | HS codes by country. |
| DeadStock <div><strong>``IEnumerable<DeadStockDTO>``</strong></div> | Dead stock information by company and branch. |
