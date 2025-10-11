---
hide:
    - toc
---
Catalog Part refers to a specific part in the Parts Catalog.  
 It is used to define the properties and information of a part.

| Property | Summary |
|----------|---------|
| ID <div><strong>``string``</strong></div> | The unique identifier for the catalog part. If an ID is not available, then the part number should be used as the ID. |
| PartNumber <div><strong>``string``</strong></div> | Each part has a unique part number that is used to identify it in the catalog and other related documents/systems. |
| PartName <div><strong>``string``</strong></div> | The name of the part as it appears in the catalog. |
| ProductGroup <div><strong>``string``</strong></div> | The product group code to which the part belongs. |
| ProductGroupDescription <div><strong>``string``</strong></div> | The description of the product group to which the part belongs. |
| BinType <div><strong>``string``</strong></div> | The type of the bin in which the part is stored. |
| DistributorPurchasePrice <div><strong>``decimal?``</strong></div> | The purchase price that the distributor pays for the part. |
| ProductCode <div><strong>``string``</strong></div> | The product code that is used to identify the part in the catalog. |
| PNC <div><strong>``string``</strong></div> | The Product Number Code (PNC) |
| Length <div><strong>``decimal?``</strong></div> | The length of the part. |
| Width <div><strong>``decimal?``</strong></div> | The width of the part. |
| Height <div><strong>``decimal?``</strong></div> | The height of the part. |
| NetWeight <div><strong>``decimal?``</strong></div> | The weight of the part. |
| CubicMeasure <div><strong>``decimal?``</strong></div> | The cubic measure of the part. |
| GrossWeight <div><strong>``decimal?``</strong></div> | The gross weight of the part. |
| Origin <div><strong>``string``</strong></div> | The country of origin of the part. |
| SupersededTo <div><strong>``IEnumerable<PartSupersessionModel>``</strong></div> | A list of all the [Supersessions](/generated/Models/Part/PartSupersessionModel.html) that the part has. |
| SupersededFrom <div><strong>``IEnumerable<PartSupersessionModel>``</strong></div> | A list of all [Supersessions](/generated/Models/Part/PartSupersessionModel.html) Where this part is the superseding part. |
| LocalDescription <div><strong>``string``</strong></div> | The localized description of the part. |
| HSCode <div><strong>``string``</strong></div> | The Harmonized System (HS) code for the part. |
| CountryData <div><strong>``IEnumerable<PartCountryDataModel>``</strong></div> | [Per Country](/generated/Models/Part/PartCountryDataModel.html) data for the part. |
