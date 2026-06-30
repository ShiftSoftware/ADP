---
hide:
    - toc
---
The distributor supply-chain leg embedded on a single `VehicleEntryModel`, for sources that
 emit one entry per VIN and carry the chain inline. Carries the
 distributor's own sale to the dealer (a  route) — its invoice number and date. The
 distributor's identity comes from `LookupOptions.DistributorCompanyID` (it is implicit/configured,
 not stored per vehicle), so only the contract fields live here.

| Property | Summary |
|----------|---------|
| InvoiceNumber <div><strong>``string``</strong></div> | The distributor leg's invoice (sales-contract) number. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The distributor leg's invoice (sales-contract) date. |
