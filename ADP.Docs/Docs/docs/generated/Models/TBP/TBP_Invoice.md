---
hide:
    - toc
---
Represents an invoice in the Third-Party Broker (TBP) system for a vehicle sale from a broker to an end customer.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this invoice. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this invoice has been deleted. |
| InvoiceDate <div><strong>``DateTimeOffset``</strong></div> | The date of the invoice. |
| IsCompleted <div><strong>``bool``</strong></div> | Indicates whether the invoice has been completed. |
| InvoiceNumber <div><strong>``long?``</strong></div> | The invoice number. |
| CustomerName <div><strong>``string``</strong></div> | The name of the end customer on this invoice. |
| CustomerPhone <div><strong>``string``</strong></div> | The phone number of the end customer. |
| CustomerIDNumber <div><strong>``string``</strong></div> | The government-issued ID number of the end customer. |
