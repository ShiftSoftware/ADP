---
hide:
    - toc
---
One intermediary leg of a vehicle's supply chain — a company (e.g. a regional importer) that moved the
 vehicle between the distributor and the selling dealer. A vehicle can pass through more than one, so
 `VehicleSaleInformation.Intermediaries` is a list. Like the distributor, an intermediary in this
 role does not make the end-customer sale (see `LookupOptions.IsEndCustomerSale`); this block is
 informational, surfaced alongside the dealer's sale.

| Property | Summary |
|----------|---------|
| CompanyID <div><strong>``string``</strong></div> | The Company Hash ID of the intermediary. |
| CompanyName <div><strong>``string``</strong></div> | The resolved intermediary company name. |
| BranchID <div><strong>``string``</strong></div> | The Branch Hash ID of the intermediary branch. |
| BranchName <div><strong>``string``</strong></div> | The resolved intermediary branch name. |
| InvoiceNumber <div><strong>``string``</strong></div> | The intermediary's invoice (or order) number for moving the vehicle toward the dealer. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date on the intermediary's invoice — this supply-chain leg's key date. |
| CityID <div><strong>``string``</strong></div> | The City Hash ID from the Identity System. |
| CityName <div><strong>``string``</strong></div> | The resolved City Name. |
