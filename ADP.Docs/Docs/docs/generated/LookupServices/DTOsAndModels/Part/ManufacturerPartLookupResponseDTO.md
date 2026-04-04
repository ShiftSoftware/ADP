---
hide:
    - toc
---
Response DTO from a manufacturer part lookup, containing the status and result data.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number that was looked up. |
| OrderType <div><strong>``ManufacturerOrderType``</strong></div> | The order type used for the lookup. |
| Status <div><strong>``ManufacturerPartLookupStatus``</strong></div> | The status of the lookup (Pending, Resolved, UnResolved). |
| ManufacturerResult <div><strong>``IEnumerable<KeyValuePairDTO>?``</strong></div> | Key-value pairs returned by the manufacturer's system. |
| Message <div><strong>``string?``</strong></div> | An optional message from the manufacturer or system. |
| Quantity <div><strong>``decimal``</strong></div> | The quantity that was requested. |
