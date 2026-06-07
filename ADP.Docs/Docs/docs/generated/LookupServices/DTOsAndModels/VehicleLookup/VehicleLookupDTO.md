---
hide:
    - toc
---
The main result returned by the vehicle lookup service.
 Contains all vehicle data including identifiers, sale information, warranty, service history, service items, accessories, and safety recalls.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) that was looked up. |
| Identifiers <div><strong>``VehicleIdentifiersDTO``</strong></div> | The vehicle's [identifiers](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleIdentifiersDTO.html) (VIN, Variant, Katashiki, Color, Trim, Brand). |
| SaleInformation <div><strong>``VehicleSaleInformation``</strong></div> | The vehicle's [sale information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleSaleInformation.html) including invoice date, dealer, and broker details. |
| PaintThicknessInspections <div><strong>``IEnumerable<PaintThicknessInspectionDTO>``</strong></div> | A list of [paint thickness inspections](/generated/LookupServices/DTOsAndModels/VehicleLookup/PaintThicknessInspectionDTO.html) performed on this vehicle. |
| PaintThicknessCertificateAvailable <div><strong>``bool``</strong></div> | Whether a  can be produced for this vehicle: the distributor has an
 invoiced entry and a PDI paint-thickness inspection exists strictly before that invoice date.
 Lets UIs decide whether to offer the certificate (e.g. a print button) without re-implementing the
 anchor logic client-side. |
| PaintThicknessCertificateUrls <div><strong>``List<PaintThicknessCertificateUrlDTO>``</strong></div> | Signed public URLs that serve the Paint Thickness Certificate — one
 [entry](/generated/LookupServices/DTOsAndModels/VehicleLookup/PaintThicknessCertificateUrlDTO.html) per print language the issuing host
 supports (the host owns the template set, so it declares the languages by what it issues
 here; list order is display order). The same kind of links the printed certificate's QR
 carries. Set only when `PaintThicknessCertificateAvailable` is true, the
 request opted in via
 `VehicleLookupRequestOptions.GeneratePaintThicknessCertificateUrls` (the endpoint's
 server-side permission check), and the host configured
 `LookupOptions.PaintThicknessCertificateUrlsResolver`. UIs render a print menu from
 the list and simply open the chosen URL in a new tab — no authenticated download round-trip. |
| IsAuthorized <div><strong>``bool``</strong></div> | Indicates whether the vehicle is authorized (has official VIN entries or SSC records). |
| Warranty <div><strong>``VehicleWarrantyDTO``</strong></div> | The vehicle's [warranty information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleWarrantyDTO.html) including start/end dates and extended warranty. |
| NextServiceDate <div><strong>``DateTime?``</strong></div> | The next scheduled service date for this vehicle. |
| ServiceHistory <div><strong>``IEnumerable<VehicleServiceHistoryDTO>``</strong></div> | The vehicle's [service history](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceHistoryDTO.html) — a list of past service invoices with labor and part lines. |
| SSC <div><strong>``IEnumerable<SscDTO>``</strong></div> | A list of [Special Service Campaigns (SSC)](/generated/LookupServices/DTOsAndModels/VehicleLookup/SscDTO.html) / safety recalls affecting this vehicle. |
| VehicleVariantInfo <div><strong>``VehicleVariantInfoDTO``</strong></div> | Parsed [variant information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleVariantInfoDTO.html) (ModelCode, SFX, ModelYear) derived from the Variant string. |
| VehicleSpecification <div><strong>``VehicleSpecificationDTO``</strong></div> | The vehicle's [technical specifications](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleSpecificationDTO.html) (model, body, engine, transmission, colors). |
| ServiceItems <div><strong>``IEnumerable<VehicleServiceItemDTO>``</strong></div> | The [service items](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceItemDTO.html) available for this vehicle (free and paid). |
| Accessories <div><strong>``IEnumerable<AccessoryDTO>``</strong></div> | The [accessories](/generated/LookupServices/DTOsAndModels/VehicleLookup/AccessoryDTO.html) installed on this vehicle. |
| BasicModelCode <div><strong>``string``</strong></div> | The basic model code extracted from the Katashiki (first segment before the hyphen, with trailing L/R removed). |
