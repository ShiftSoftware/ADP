---
hide:
    - toc
---
Represents a vehicle transfer record stored in the vehicle aggregate.
 Tracks the movement of a vehicle from one broker to another, with a unique transfer number.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this transfer. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) being transferred. |
| TransferDate <div><strong>``DateTime``</strong></div> | The date the transfer was executed. |
| TransferNumber <div><strong>``long``</strong></div> | The unique transfer number for tracking. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this transfer has been deleted. |
| FromBrokerID <div><strong>``long?``</strong></div> | The broker ID of the seller (source) in this transfer. |
| ToBrokerID <div><strong>``long?``</strong></div> | The broker ID of the buyer (destination) in this transfer. |
