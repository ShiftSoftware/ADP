---
hide:
    - toc
---
Represents a vehicle entry from the dealer system as it appears in the TBP stock context.
 Used to validate broker stock quantities against dealer records.

| Property | Summary |
|----------|---------|
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The invoice date from the dealer system. |
| CustomerAccountNumber <div><strong>``string``</strong></div> | The customer account number on the dealer invoice. |
| Status <div><strong>``string``</strong></div> | The order status from the dealer system. |
| LineStatus <div><strong>``string``</strong></div> | The line item status from the dealer system. |
| Location <div><strong>``string``</strong></div> | The location/warehouse identifier from the dealer system. |
| CompanyID <div><strong>``long?``</strong></div> | The company ID from the dealer system. |
| Region <div><strong>``string?``</strong></div> | The region identifier from the dealer system. |
