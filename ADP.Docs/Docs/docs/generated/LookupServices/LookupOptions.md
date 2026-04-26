---
hide:
    - toc
---
The main configuration class for the lookup services.
 Contains resolver delegates for resolving images, names, and prices; feature flags; warranty settings; and storage options.

| Property | Summary |
|----------|---------|
| ServiceItemImageUrlResolver <div><strong>``Func<LookupOptionResolverModel<Dictionary<string,string>>, ValueTask<string?>>?``</strong></div> | Resolver delegate that converts a multilingual image dictionary to a resolved image URL for service items. |
| BrandStandardWarrantyPeriodsInYears <div><strong>``Dictionary<long?, int>``</strong></div> | A dictionary mapping brand IDs to their standard warranty period in years. |
| PaintThickneesImageUrlResolver <div><strong>``Func<LookupOptionResolverModel<string>,ValueTask<string?>>?``</strong></div> | Resolver delegate that converts a paint thickness image path to a full URL. |
| AccessoryImageUrlResolver <div><strong>``Func<LookupOptionResolverModel<string>, ValueTask<string?>>?``</strong></div> | Resolver delegate that converts an accessory image path to a full URL. |
| CompanyLogoImageResolver <div><strong>``Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>?``</strong></div> | Resolver delegate that resolves company logo images. |
| PartLocationNameResolver <div><strong>``Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a part location identifier to a human-readable name. |
| CountryFromBranchIDResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<(long? countryID, string countryName)?>>?``</strong></div> | Resolver delegate that resolves a branch ID to its country ID and name. |
| CountryNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a country ID to its name. |
| RegionNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a region ID to its name. |
| CompanyNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company ID to its name. |
| CompanyBranchNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company branch ID to its name. |
| CompanyLogoResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company ID to its logo URL. |
| PartLookupPriceResolver <div><strong>``Func<LookupOptionResolverModel<PartLookupPriceResoulverModel>, ValueTask<(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices)>>?``</strong></div> | Resolver delegate that processes and returns part pricing (distributor purchase price and per-region prices). |
| PartLookupStocksResolver <div><strong>``Func<LookupOptionResolverModel<IEnumerable<StockPartDTO>>, ValueTask<IEnumerable<StockPartDTO>>>?``</strong></div> | Resolver delegate that processes and returns part stock availability data. |
| IncludeInactivatedFreeServiceItems <div><strong>``bool``</strong></div> | Whether to include free service items that have not yet been activated (e.g., awaiting warranty activation). |
| WarrantyStartDateDefaultsToInvoiceDate <div><strong>``bool``</strong></div> | Whether the warranty start date should default to the invoice date when no explicit activation date is set. Defaults to true. |
| SigningSecretKey <div><strong>``string``</strong></div> | The HMAC secret key used for signing service item claim requests. |
| SignatureValidityDuration <div><strong>``TimeSpan``</strong></div> | How long a generated claim signature remains valid. |
| VehicleInspectionPreClaimVoucherPrintingURLResolver <div><strong>``Func<LookupOptionResolverModel<(string VehicleInspectionID, string ServiceItemID)>, ValueTask<string?>>?``</strong></div> | Resolver delegate that generates a pre-claim voucher printing URL for vehicle inspection-based claims. |
| ServiceActivationPreClaimVoucherPrintingURLResolver <div><strong>``Func<LookupOptionResolverModel<(string ServiceActivationID, string ServiceItemID)>, ValueTask<string?>>?``</strong></div> | Resolver delegate that generates a pre-claim voucher printing URL for service activation-based claims. |
| StandardItemClaimWarnings <div><strong>``List<VehicleItemWarning>``</strong></div> | Standard warning messages displayed to users before claiming any service item. |
| DistributorStockPartLookupQuantityThreshold <div><strong>``int?``</strong></div> | The minimum stock quantity threshold for distributor stock lookup. Quantities below this are reported as QuantityNotWithinLookupThreshold. |
| ShowPartLookupStockQauntity <div><strong>``bool``</strong></div> | Whether to show the actual stock quantity in part lookup results (vs. just availability status). Defaults to false. |
| EnableManufacturerLookup <div><strong>``bool``</strong></div> | Whether the manufacturer part lookup feature is enabled. |
| CatalogPartShouldComeFromStock <div><strong>``bool``</strong></div> | Whether catalog part data should only come from stock records (vs. the dedicated catalog). |
| LookupBrokerStock <div><strong>``bool``</strong></div> | Whether to look up broker stock data for vehicles. |
| VehicleLookupStorageSource <div><strong>``StorageSources``</strong></div> | The storage backend to use for vehicle lookups (CosmosDB or DuckDB). |
