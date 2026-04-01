---
hide:
    - toc
---
Represents an invoice for paid service work performed on a vehicle.
 Contains the invoice metadata and a collection of  detailing the services performed.

| Property | Summary |
|----------|---------|
| InvoiceDate <div><strong>``DateTime``</strong></div> | The date of the service invoice. |
| StartDate <div><strong>``DateTime``</strong></div> | The date the service was started. |
| InvoiceNumber <div><strong>``long``</strong></div> | The invoice number for this paid service. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this invoice has been deleted. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this invoice is for. |
| Lines <div><strong>``IEnumerable<PaidServiceInvoiceLineModel>``</strong></div> | The [line items](/generated/Models/Vehicle/PaidServiceInvoiceLineModel.html) on this invoice, each representing a service performed. |
| BrandHashID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| IntegrationID <div><strong>``string``</strong></div> | An external identifier used for system-to-system integration. |
