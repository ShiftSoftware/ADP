---
hide:
    - toc
---
Represents a part's stock availability at a specific location/warehouse.

| Property | Summary |
|----------|---------|
| QuantityLookUpResult <div><strong>``QuantityLookUpResults``</strong></div> | The result of the stock quantity lookup (e.g., Available, NotAvailable, PartiallyAvailable). |
| LocationID <div><strong>``string``</strong></div> | The location/warehouse identifier. |
| LocationName <div><strong>``string``</strong></div> | The resolved location/warehouse name. |
| AvailableQuantity <div><strong>``decimal?``</strong></div> | The available quantity at this location (only shown if configured). |
