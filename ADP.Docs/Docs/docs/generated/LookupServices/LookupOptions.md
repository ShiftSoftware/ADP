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
| PaintThicknessCertificateSerialNumberResolver <div><strong>``Func<LookupOptionResolverModel<string>, ValueTask<string?>>?``</strong></div> | Resolver delegate that derives the Paint Thickness Certificate's serial number from the chosen
 inspection's `id`. Must be deterministic — the same inspection should always produce the same
 serial so re-printed certificates match. When unset, `PaintThicknessCertificateModel.SerialNumber`
 is `null`. |
| PaintThicknessCertificateUrlsResolver <div><strong>``Func<LookupOptionResolverModel<string>, ValueTask<List<PaintThicknessCertificateUrlDTO>?>>?``</strong></div> | Resolver delegate that generates the Paint Thickness Certificate's signed public URLs for a
 VIN — one entry per print language the host supports (the host owns the certificate
 templates, so it declares the supported set by what it returns here; list order is display
 order). The same kind of links the printed certificate's QR carries. Invoked only when the
 certificate is available and the request opted in via
 `VehicleLookupRequestOptions.GeneratePaintThicknessCertificateUrls`. Receives the
 VIN as the value and the request's language code. When unset,
 `VehicleLookupDTO.PaintThicknessCertificateUrls` stays null. |
| AccessoryImageUrlResolver <div><strong>``Func<LookupOptionResolverModel<string>, ValueTask<string?>>?``</strong></div> | Resolver delegate that converts an accessory image path to a full URL. |
| CompanyLogoImageResolver <div><strong>``Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>?``</strong></div> | Resolver delegate that resolves company logo images. |
| PartLocationNameResolver <div><strong>``Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a part location identifier to a human-readable name. |
| CountryNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a country ID to its name. |
| BrandNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a brand ID to its name. Used by the diagnostic trace renderer; not required for normal lookups. |
| RegionNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a region ID to its name. |
| CityNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a city ID to its name. |
| CityFromBranchIDResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<long?>>?``</strong></div> | Resolver delegate that resolves a company branch ID to the city ID the branch belongs to. |
| CompanyNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company ID to its name. |
| CompanyBranchNameResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company branch ID to its name. |
| CompanyLogoResolver <div><strong>``Func<LookupOptionResolverModel<long?>, ValueTask<string?>>?``</strong></div> | Resolver delegate that resolves a company ID to its logo URL. |
| PartLookupPriceResolver <div><strong>``Func<LookupOptionResolverModel<PartLookupPriceResoulverModel>, ValueTask<(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices)>>?``</strong></div> | Resolver delegate that processes and returns part pricing (distributor purchase price and per-region prices). |
| PartLookupStocksResolver <div><strong>``Func<LookupOptionResolverModel<IEnumerable<StockPartDTO>>, ValueTask<IEnumerable<StockPartDTO>>>?``</strong></div> | Resolver delegate that processes and returns part stock availability data. |
| EnableSSCPartAvailability <div><strong>``bool``</strong></div> | Master on/off switch for SSC (safety recall) part stock-availability enrichment. Defaults to `false`
 (off): even when `SSCPartStockScopeResolver` is wired, no stock is read and every
 `ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup.SSCPartDTO.IsAvailable` is left
 `null` — the UI renders a neutral "not checked" chip. Set to `true` to turn the feature on for a
 deployment once its stock-scope mapping is confirmed. This lets the resolver ship wired but dormant, so the
 NuGet can be deployed and hosts upgraded without changing behaviour until a host opts in. |
| SSCPartStockScopeResolver <div><strong>``Func<LookupOptionResolverModel<SSCPartAvailabilityScopeRequest>, ValueTask<IReadOnlyCollection<string>?>>?``</strong></div> | Resolves the stock `ShiftSoftware.ADP.Models.Part.StockPartModel.Location` key(s) whose
 inventory counts as "in stock" for the current requester when checking SSC (safety recall) part
 availability. This is a deployment-specific, logic-free identity→scope mapping and the ONLY host
 responsibility for SSC part availability: e.g. a region-keyed deployment returns the requester's region
 key (its stock rows carry `Location = the region key`); a warehouse-keyed one returns the
 requester's warehouse / business-unit `Location`(s). The resolver must perform NO stock query and NO
 availability math — ADP owns those decisions (see the SSC part-availability enricher). Return
 `null` or an empty collection for requesters who should not see stock availability (anonymous, bulk
 and reporting callers); their SSC parts are left unchecked (`IsAvailable = null`) and no stock read
 runs. Obtain the current requester's region/branch from `LookupOptionResolverModel{T}.Services`. |
| PartNumberStorageKeyResolver <div><strong>``Func<string, string>?``</strong></div> | Optional map from a part number in its standard / manufacturer form (as it appears on an SSC recall, e.g.
 `04007-07212`) to the exact key used to store that part in this deployment's Cosmos Parts container,
 so SSC part availability can be matched against stock. Deployments whose stored key differs from the
 manufacturer form must set this — e.g. a deployment that stores parts T-prefixed and dash-stripped maps
 (`04007-07212` → `T0400707212`); one that keeps the hyphen maps (`04007-07212` →
 `04007-07212`). When unset, ADP applies a neutral default (dashes removed, upper-cased). Must be a
 pure, deterministic function; ADP calls it once per distinct SSC part number. |
| IncludeInactivatedFreeServiceItems <div><strong>``bool``</strong></div> | Whether to include free service items that have not yet been activated (e.g., awaiting warranty activation). |
| TreatServiceItemExpiryAsEndOfDay <div><strong>``bool``</strong></div> | When enabled, a service item stays claimable through the whole of its expiry date — until
 `23:59:59.9999999` UTC — instead of expiring the moment that date begins. |
| RequireAllocationForActivation <div><strong>``bool``</strong></div> | When enabled, warranty activation is only offered to a requester whose company has a vehicle entry for the
 vehicle (i.e. it has been allocated/shipped/delivered to them). When activation is due but the vehicle is not
 allocated to the requesting company, `VehicleWarrantyDTO.ActivationStatus` becomes `BlockedNotAllocated`
 instead of `Required`. Requires the caller to supply `VehicleLookupRequestOptions.RequestingCompanyID`;
 with no requesting company the activation affordance is suppressed. Layered on top of
 `IncludeInactivatedFreeServiceItems`; defaults to false. |
| WarrantyStartDateDefaultsToInvoiceDate <div><strong>``bool``</strong></div> | Whether the warranty start date should default to the invoice date when no explicit activation date is set. Defaults to true. |
| DistributorCompanyID <div><strong>``long?``</strong></div> | The Identity `CompanyID` of the distributor company. A VIN can have multiple `VehicleEntries` —
 one for the distributor and one (or more) for the selling dealer(s) — and the dealer's sale invoice date
 is later than the distributor's. Distributor-scoped logic such as the Paint Thickness Certificate anchors
 on the distributor's invoice date, so it selects the entry whose `CompanyID` equals this value.
  for the Paint Thickness Certificate: if this is unset, or the VIN has no invoiced entry for
 this company, no certificate is produced — it never falls back to a dealer's invoice. |
| IntermediaryCompanyIDs <div><strong>``List<long>``</strong></div> | The Identity `CompanyID`s of any intermediary companies (e.g. a regional importer that sits between
 the distributor and the dealer). A VIN can pass through more than one, so this is a list. Like the
 distributor, an intermediary normally only moves the vehicle toward the dealer, so its `VehicleEntry`
 must not anchor warranty/free-service dates or service-item eligibility — unless that entry is marked as a
 direct sale to a customer. See `IsEndCustomerSale`. Defaults to empty (no intermediaries). |
| DirectEndCustomerSaleAccountNumbersByCompany <div><strong>``Dictionary<long, HashSet<string>>``</strong></div> | Per company, the `AccountNumber`(s) that mark a `VehicleEntry` as a  even though that company is a supply-chain company. Today this is how a distributor selling
 straight to a customer, instead of shipping to a dealer, is recognised — see `IsEndCustomerSale`. |
| SigningSecretKey <div><strong>``string``</strong></div> | The HMAC secret key used for signing service item claim requests. |
| SignatureValidityDuration <div><strong>``TimeSpan``</strong></div> | How long a generated claim signature remains valid. |
| TimeProvider <div><strong>``TimeProvider``</strong></div> | Clock used for all time-dependent lookup output — service-item status/claimability, warranty-active flags and `SignatureExpiry`. Defaults to `TimeProvider.System`; override with a fixed provider to produce deterministic output for sample/doc generation or tests. |
| VehicleInspectionPreClaimVoucherPrintingURLResolver <div><strong>``Func<LookupOptionResolverModel<(string VehicleInspectionID, string ServiceItemID)>, ValueTask<string?>>?``</strong></div> | Resolver delegate that generates a pre-claim voucher printing URL for vehicle inspection-based claims. |
| ServiceActivationPreClaimVoucherPrintingURLResolver <div><strong>``Func<LookupOptionResolverModel<(string ServiceActivationID, string ServiceItemID)>, ValueTask<string?>>?``</strong></div> | Resolver delegate that generates a pre-claim voucher printing URL for service activation-based claims. |
| StandardItemClaimWarnings <div><strong>``List<VehicleItemWarning>``</strong></div> | Standard warning messages displayed to users before claiming any service item. |
| SkippedItemsClaimWarningResolver <div><strong>``Func<LookupOptionResolverModel<(VehicleServiceItemDTO ItemBeingClaimed, List<VehicleServiceItemDTO> SkippedItems)>, ValueTask<VehicleItemWarning?>>?``</strong></div> | Resolver delegate that builds the warning shown when claiming a free sequential item would cancel lower-mileage pending items (e.g. claiming the 50K service while the 45K is still pending). Receives the item being claimed and the pending items that claiming it would cancel (the pre-claim mirror of dynamic cancellation). Return null to suppress the warning. When not configured, no skipped-items warning is attached. |
| UnInvoicedBrokerClaimWarningResolver <div><strong>``Func<LookupOptionResolverModel<VehicleBrokerSaleInformation>, ValueTask<VehicleItemWarning?>>?``</strong></div> | Resolver delegate that builds the warning shown when claiming items on a vehicle that is held by a broker (TBP) that has not invoiced it yet. Receives the broker sale information. Return null to suppress the warning. When not configured, no broker warning is attached. |
| DistributorStockPartLookupQuantityThreshold <div><strong>``int?``</strong></div> | The minimum stock quantity threshold for distributor stock lookup. Quantities below this are reported as QuantityNotWithinLookupThreshold. |
| ShowPartLookupStockQauntity <div><strong>``bool``</strong></div> | Whether to show the actual stock quantity in part lookup results (vs. just availability status). Defaults to false. |
| EnableManufacturerLookup <div><strong>``bool``</strong></div> | Whether the manufacturer part lookup feature is enabled. |
| CatalogPartShouldComeFromStock <div><strong>``bool``</strong></div> | Whether catalog part data should only come from stock records (vs. the dedicated catalog). |
| LookupBrokerStock <div><strong>``bool``</strong></div> | Whether to look up broker stock data for vehicles. |
| VehicleLookupStorageSource <div><strong>``StorageSources``</strong></div> | The storage backend to use for vehicle lookups (CosmosDB or DuckDB). |
