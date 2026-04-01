---
hide:
    - toc
---
Represents the cost of a  for a specific vehicle variant or package.
 A service item may have different costs depending on the vehicle's variant, Katashiki, or package code.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this service item cost entry. |
| ServiceItemID <div><strong>``long?``</strong></div> | The ID of the [Service Item](/generated/Models/Vehicle/ServiceItemModel.html) this cost applies to. |
| Variant <div><strong>``string``</strong></div> | The vehicle variant code this cost applies to. |
| Katashiki <div><strong>``string``</strong></div> | The Katashiki (model-specific identifier) this cost applies to. |
| PackageCode <div><strong>``string``</strong></div> | The package code this cost applies to. |
| Cost <div><strong>``decimal?``</strong></div> | Cost in dollars. |
