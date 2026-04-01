---
hide:
    - toc
---
Represents a claim made against a  for a specific vehicle.
 Tracks the claim details including cost, associated inspection, and job/invoice references.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this claim is for. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| ClaimDate <div><strong>``DateTimeOffset``</strong></div> | The date when this service item was claimed. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this claim has been deleted. |
| ServiceItemID <div><strong>``string``</strong></div> | The ID of the [Service Item](/generated/Models/Vehicle/ServiceItemModel.html) being claimed. |
| VehicleInspectionID <div><strong>``string``</strong></div> | The ID of the [Vehicle Inspection](/generated/Models/Vehicle/VehicleInspectionModel.html) associated with this claim, if any. |
| Cost <div><strong>``decimal``</strong></div> | The cost of this claim. |
| PackageCode <div><strong>``string``</strong></div> | The package code grouping this claim with related service items. |
| JobNumber <div><strong>``string``</strong></div> | The job number from the dealer's service system. |
| InvoiceNumber <div><strong>``string``</strong></div> | The invoice number associated with this claim. |
| QRCode <div><strong>``string``</strong></div> | A QR code identifier for this claim. |
