---
hide:
    - toc
---
Represents a warranty claim submitted for a vehicle repair.
 Tracks the claim lifecycle from submission through distributor and manufacturer processing, including repair details and labor operations.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this warranty claim is for. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this warranty claim has been deleted. |
| RepairDate <div><strong>``DateTime?``</strong></div> | The date the repair was performed. |
| ClaimStatus <div><strong>``ClaimStatus``</strong></div> | The current status of this warranty claim in the processing lifecycle. |
| ManufacturerStatus <div><strong>``WarrantyManufacturerClaimStatus``</strong></div> | The manufacturer's status for this warranty claim. |
| DistributorComment <div><strong>``string``</strong></div> | Comments added by the distributor during claim processing. |
| LaborOperationNumberMain <div><strong>``string``</strong></div> | The main labor operation number for this claim. |
| ClaimNumber <div><strong>``string``</strong></div> | The unique claim number assigned to this warranty claim. |
| InvoiceNumber <div><strong>``string``</strong></div> | The invoice number associated with this warranty claim. |
| DealerClaimNumber <div><strong>``string``</strong></div> | The dealer's own claim number for internal tracking. |
| DateOfReceipt <div><strong>``DateTime?``</strong></div> | The date the claim was received by the distributor. |
| WarrantyType <div><strong>``string``</strong></div> | The type of warranty coverage (e.g., Standard, Extended). |
| BrandHashID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
| DeliveryDate <div><strong>``DateTime?``</strong></div> | The vehicle delivery date to the customer. |
| RepairCompletionDate <div><strong>``DateTime?``</strong></div> | The date the repair was completed. |
| Odometer <div><strong>``int?``</strong></div> | The vehicle's odometer reading at the time of the warranty repair. |
| RepairOrderNumber <div><strong>``string``</strong></div> | The repair order number from the dealer's service system. |
| ProcessDate <div><strong>``DateTime?``</strong></div> | The date this claim was processed by the manufacturer. |
| DistributorProcessDate <div><strong>``DateTime?``</strong></div> | The date this claim was processed by the distributor. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| LaborLines <div><strong>``IEnumerable<WarrantyClaimLaborLineModel>``</strong></div> | The [labor lines](/generated/Models/Vehicle/WarrantyClaimLaborLineModel.html) associated with this warranty claim. |
