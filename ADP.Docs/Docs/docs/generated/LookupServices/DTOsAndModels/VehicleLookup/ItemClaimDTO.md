---
hide:
    - toc
---
Represents a service item claim request/result, containing the claim details and associated vehicle information.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string?``</strong></div> | The Vehicle Identification Number. |
| Invoice <div><strong>``string?``</strong></div> | The invoice number provided for the claim. |
| JobNumber <div><strong>``string?``</strong></div> | The job number provided for the claim. |
| QRCode <div><strong>``string?``</strong></div> | The QR code scanned for the claim. |
| SaleInformation <div><strong>``VehicleSaleInformation?``</strong></div> | The vehicle's [sale information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleSaleInformation.html). |
| ServiceItem <div><strong>``VehicleServiceItemDTO?``</strong></div> | The [service item](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceItemDTO.html) being claimed. |
| Identifiers <div><strong>``VehicleIdentifiersDTO?``</strong></div> | The vehicle's [identifiers](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleIdentifiersDTO.html). |
| VehicleVariantInfo <div><strong>``VehicleVariantInfoDTO?``</strong></div> | The vehicle's [variant information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleVariantInfoDTO.html). |
| VehicleSpecification <div><strong>``VehicleSpecificationDTO?``</strong></div> | The vehicle's [technical specifications](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleSpecificationDTO.html). |
