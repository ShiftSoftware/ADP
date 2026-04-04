---
hide:
    - toc
---
Represents a line item within a Paid Service Invoice.
 Each line corresponds to a specific Service Item that was performed and paid for.

| Property | Summary |
|----------|---------|
| ServiceItemID <div><strong>``string``</strong></div> | The ID of the [Service Item](/generated/Models/Vehicle/ServiceItemModel.html) that was performed. |
| Cost <div><strong>``decimal``</strong></div> | The cost charged for this service line item. |
| ExpireDate <div><strong>``DateTime?``</strong></div> | The expiration date for the service coverage provided by this line item. |
| PackageCode <div><strong>``string``</strong></div> | The package code grouping this line item with related services. |
| ServiceItem <div><strong>``ServiceItemModel``</strong></div> | The [Service Item](/generated/Models/Vehicle/ServiceItemModel.html) details associated with this line. |
| IntegrationID <div><strong>``string``</strong></div> | An external identifier used for system-to-system integration. |
