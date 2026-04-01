---
hide:
    - toc
---
The result of a part stock quantity lookup at a specific location.

| Value | Summary |
|-------|---------|
| LookupIsSkipped | Stock lookup was skipped (not configured or not applicable). |
| Available | The requested quantity is fully available. |
| PartiallyAvailable | Some quantity is available but less than requested. |
| NotAvailable | The part is not available at this location. |
| QuantityNotWithinLookupThreshold | The lookup quantity is below the configured threshold for display. |
