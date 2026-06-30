---
hide:
    - toc
---
The intermediary supply-chain leg embedded on a single `VehicleEntryModel`. Used by sources
 that emit one entry per VIN and carry the supply chain inline, rather
 than as a separate entry per leg. Represents a company (e.g. a regional importer) that moved the vehicle
 between the distributor and the selling dealer. The intermediary never makes the end-customer sale.

| Property | Summary |
|----------|---------|
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID of the intermediary. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID of the intermediary branch. |
| InvoiceNumber <div><strong>``string``</strong></div> | The intermediary leg's invoice (sales-contract) number. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The intermediary leg's invoice (sales-contract) date. |
