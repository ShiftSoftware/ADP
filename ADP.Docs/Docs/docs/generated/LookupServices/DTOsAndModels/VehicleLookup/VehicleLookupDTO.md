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
