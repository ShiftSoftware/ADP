---
hide:
    - toc
---
Why an item is on screen without being claimable, and what the customer would have to do about it.

| Property | Summary |
|----------|---------|
| State <div><strong>``VehicleServiceItemLockState``</strong></div> | Which of the two unclaimable states this item is in. |
| Prerequisites <div><strong>``List<VehicleServiceItemPrerequisiteDTO>``</strong></div> | The services the customer must have had for this item to unlock, each with whether it has happened. Empty when the item is unclaimable for a reason that decomposes into no steps. |
