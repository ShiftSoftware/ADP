---
hide:
    - toc
---
Represents a service item available for a vehicle — includes its type (free/paid), status (pending/processed/expired),
 validity period, cost, claimability, and an HMAC signature for secure claiming.

| Property | Summary |
|----------|---------|
| Group <div><strong>``VehicleServiceItemGroup``</strong></div> | The [group](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceItemGroup.html) this service item belongs to (for UI tab grouping). |
| ShowDocumentUploader <div><strong>``bool``</strong></div> | Whether to show a document uploader when claiming this item. |
| Warnings <div><strong>``List<VehicleItemWarning>?``</strong></div> | A list of [warnings](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleItemWarning.html) to display before claiming this item. |
| PrintUrl <div><strong>``string?``</strong></div> | URL for printing the service item certificate. |
| DocumentUploaderIsRequired <div><strong>``bool``</strong></div> | Whether the document uploader is required (not just shown) when claiming. |
| Name <div><strong>``string``</strong></div> | The localized name of the service item. |
| Description <div><strong>``string``</strong></div> | The localized description of the service item. |
| Title <div><strong>``string``</strong></div> | The localized printout title. |
| Image <div><strong>``string``</strong></div> | The localized image URL for the service item. |
| Type <div><strong>``string``</strong></div> | The human-readable type label (e.g., "Free", "Paid"). |
| TypeEnum <div><strong>``VehcileServiceItemTypes``</strong></div> | The service item type as an enum value. |
| ActivatedAt <div><strong>``DateTime``</strong></div> | The date this service item was activated for this vehicle. |
| ExpiresAt <div><strong>``DateTime?``</strong></div> | The date this service item expires. Null if no expiration. |
| Status <div><strong>``string``</strong></div> | The human-readable status label (e.g., "Pending", "Processed", "Expired"). |
| StatusEnum <div><strong>``VehcileServiceItemStatuses``</strong></div> | The service item status as an enum value. |
| CampaignID <div><strong>``long?``</strong></div> | The ID of the campaign this item belongs to. |
| CampaignUniqueReference <div><strong>``string``</strong></div> | The unique reference of the parent campaign. |
| PackageCode <div><strong>``string``</strong></div> | The package code grouping related service items together. |
| Cost <div><strong>``decimal?``</strong></div> | The cost of this service item for this vehicle. |
| ClaimDate <div><strong>``DateTimeOffset?``</strong></div> | The date this item was claimed, if already claimed. |
| ModelCostID <div><strong>``long?``</strong></div> | The model-specific cost ID used when costing type is 'Per Model'. |
| ServiceItemID <div><strong>``string``</strong></div> | The unique identifier of the service item definition. |
| PaidServiceInvoiceLineID <div><strong>``string?``</strong></div> | The paid service invoice line ID, if this is a paid service item. |
| CompanyName <div><strong>``string``</strong></div> | The localized company name that performed or will perform the service. |
| InvoiceNumber <div><strong>``string``</strong></div> | The invoice number associated with the claim. |
| JobNumber <div><strong>``string``</strong></div> | The job number associated with the claim. |
| MaximumMileage <div><strong>``long?``</strong></div> | The maximum mileage for sequential validity calculations. |
| Claimable <div><strong>``bool``</strong></div> | Whether this service item can currently be claimed. |
| ClaimingMethodEnum <div><strong>``ClaimableItemClaimingMethod``</strong></div> | The method used to claim this item (e.g., QR Code scan, Invoice + Job Number). |
| VehicleInspectionID <div><strong>``string``</strong></div> | The vehicle inspection ID associated with this claim, if applicable. |
| VehicleInspectionTypeID <div><strong>``string``</strong></div> | The vehicle inspection type ID required for claiming, if applicable. |
| Signature <div><strong>``string``</strong></div> | The HMAC signature used to securely validate claim requests. |
| SignatureExpiry <div><strong>``DateTime``</strong></div> | The UTC expiry time of the signature. |
