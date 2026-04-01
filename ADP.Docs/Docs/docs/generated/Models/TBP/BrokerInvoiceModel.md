---
hide:
    - toc
---
Represents a broker invoice record stored in the vehicle aggregate.
 Tracks the sale of a vehicle from a broker to an end customer, including customer references.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this invoice is for. |
| InvoiceDate <div><strong>``DateTime``</strong></div> | The date of the broker invoice. |
| BrokerCustomerID <div><strong>``long?``</strong></div> | The ID of the official broker customer who purchased the vehicle. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this invoice has been deleted. |
| ID <div><strong>``long``</strong></div> | The unique identifier for this invoice. |
| InvoiceNumber <div><strong>``long``</strong></div> | The invoice number. |
| NonOfficialBrokerCustomerID <div><strong>``long?``</strong></div> | The ID of a non-official broker customer, if the buyer is not in the official customer database. |
