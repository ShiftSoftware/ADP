---
hide:
    - toc
---
Represents the initial vehicle record when a vehicle first enters a broker's inventory.
 This is created when a broker receives a vehicle before any dealer system entries exist.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this initial vehicle record. |
| BrokerID <div><strong>``long``</strong></div> | The ID of the broker that received this vehicle. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN). |
| Deleted <div><strong>``bool``</strong></div> | Indicates whether this initial vehicle record has been deleted. |
