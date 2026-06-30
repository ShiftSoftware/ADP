---
hide:
    - toc
---
The distributor leg of a vehicle's supply chain — the company that imported/distributed the vehicle to the
 selling dealer. The distributor never makes the end-customer sale (see
 `LookupOptions.IsEndCustomerSaleCompany`), so this block is purely informational and is
 surfaced alongside the dealer's sale on `VehicleSaleInformation`. Null when the vehicle has no
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
