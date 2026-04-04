---
hide:
    - toc
---
Represents a vehicle transfer between two brokers in the TBP system.
 Transfers affect the stock quantity calculation — the seller loses one unit and the buyer gains one.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this transfer. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this transfer has been deleted. |
| SellerBrokerID <div><strong>``long``</strong></div> | The broker ID of the seller in this transfer. |
| BuyerBrokerID <div><strong>``long``</strong></div> | The broker ID of the buyer in this transfer. |
| TransferDate <div><strong>``DateTimeOffset``</strong></div> | The date the transfer was executed. |
