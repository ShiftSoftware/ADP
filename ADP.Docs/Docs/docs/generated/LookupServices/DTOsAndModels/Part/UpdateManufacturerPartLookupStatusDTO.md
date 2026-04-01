---
hide:
    - toc
---
DTO for updating the status of a manufacturer part lookup request.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number that was looked up. |
| Status <div><strong>``ManufacturerPartLookupStatus``</strong></div> | The new status to set (Pending, Resolved, UnResolved). |
| ManufacturerResult <div><strong>``IEnumerable<KeyValuePairDTO>?``</strong></div> | The manufacturer result key-value pairs, if resolved. |
