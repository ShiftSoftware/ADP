---
hide:
    - toc
---
The print/display data model for a vehicle's : the readings from the
 latest PDI (Pre-Delivery Inspection) paint-thickness inspection taken  the distributor's sale
 invoice date, plus vehicle header information.

| Property | Summary |
|----------|---------|
| SerialNumber <div><strong>``string``</strong></div> | The certificate's auto-generated serial number (e.g. `"3F09A-12B45"`), derived
 deterministically from the chosen inspection via
 `LookupOptions.PaintThicknessCertificateSerialNumberResolver` — the same inspection
 always yields the same serial, so re-prints match. `null` when no resolver is configured. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number. |
| ModelDescription <div><strong>``string``</strong></div> | A human-readable description of the vehicle model. |
| ModelCode <div><strong>``string``</strong></div> | The model code that identifies the vehicle model. |
| ModelYear <div><strong>``string``</strong></div> | The model year of the vehicle. |
| ExteriorColorCode <div><strong>``string``</strong></div> | The exterior color code of the vehicle. |
| ExteriorColorDescription <div><strong>``string``</strong></div> | The resolved exterior color description (e.g. "Pearl White"); `null` when unavailable. |
| InteriorColorCode <div><strong>``string``</strong></div> | The interior (trim) color code of the vehicle. |
| InteriorColorDescription <div><strong>``string``</strong></div> | The resolved interior (trim) color description; `null` when unavailable. |
| Katashiki <div><strong>``string``</strong></div> | The Katashiki (manufacturer model-specific identifier). |
| VariantCode <div><strong>``string``</strong></div> | The variant code of the vehicle within its model range. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The sale invoice date — the anchor the inspection is selected against. |
| InvoiceNumber <div><strong>``string``</strong></div> | The sale invoice number. |
| CompanyName <div><strong>``string``</strong></div> | The resolved selling company (dealer) name. |
| BranchName <div><strong>``string``</strong></div> | The resolved selling branch name. |
| CityName <div><strong>``string``</strong></div> | The resolved city name. |
| CountryName <div><strong>``string``</strong></div> | The resolved country name. |
| Source <div><strong>``string``</strong></div> | The source of the chosen inspection (e.g. "PDI"). |
| InspectionDate <div><strong>``DateTime?``</strong></div> | The date the chosen paint thickness inspection was performed. |
| Readings <div><strong>``List<PaintThicknessCertificateReadingModel>``</strong></div> | The per-panel [readings](/generated/LookupServices/DTOsAndModels/VehicleLookup/PaintThicknessCertificateReadingModel.html) for the certificate table. |
| PanelImages <div><strong>``List<string>``</strong></div> | All distinct resolved panel-image URLs, for the landing-page gallery. |
