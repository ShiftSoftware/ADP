---
hide:
    - toc
---
The distributor leg of a vehicle's supply chain — the company that imported/distributed the vehicle. This is
 a role fact and holds however the vehicle was later sold: a distributor that sold straight to a customer is
 still the distributor, and is reported here as well as being the resolved sale. The block is purely
 informational and surfaced alongside the sale on `VehicleSaleInformation`; it never changes the
 resolved sale. `InvoiceNumber`/`InvoiceDate` are the distributor's own outbound
 sale — to a dealer, an intermediary, or (on a direct sale) the end customer. Null when the vehicle has no
 distributor leg, or none is configured for the deployment.

| Property | Summary |
|----------|---------|
| CompanyID <div><strong>``string``</strong></div> | The Company Hash ID of the distributor. |
| CompanyName <div><strong>``string``</strong></div> | The resolved distributor company name. |
| BranchID <div><strong>``string``</strong></div> | The Branch Hash ID of the distributor branch. |
| BranchName <div><strong>``string``</strong></div> | The resolved distributor branch name. |
| InvoiceNumber <div><strong>``string``</strong></div> | The distributor's invoice (or order) number for moving the vehicle toward the dealer. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date on the distributor's invoice — this supply-chain leg's key date. |
| CityID <div><strong>``string``</strong></div> | The City Hash ID from the Identity System. |
| CityName <div><strong>``string``</strong></div> | The resolved City Name. |
