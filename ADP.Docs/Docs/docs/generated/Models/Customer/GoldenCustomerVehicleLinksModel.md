---
hide:
    - toc
---
The by-customer projection of vehicle ownership: every VIN a `GoldenCustomerModel`
 has been linked to, with role, period, and evidence — the customer-360 "vehicles owned" read.
 One document per golden identity that has any vehicle links.
 Lives in the same logical partition as the golden document itself (no CompanyID — a golden
 identity may span dealers — and CustomerID = GoldenCustomerID), so a customer page can fetch
 the golden and its vehicle links from one partition.

| Property | Summary |
|----------|---------|
| GoldenCustomerID <div><strong>``string``</strong></div> | The [Golden Customer](/generated/Models/Customer/GoldenCustomerModel.html) these links belong to. |
| CustomerID <div><strong>``string``</strong></div> | The customer identifier. Same value as `GoldenCustomerID` (keeps the document in the golden's partition). |
| Links <div><strong>``IEnumerable<GoldenVehicleLinkModel>``</strong></div> | The customer's vehicle links, one per VIN, ordered by VIN. |
