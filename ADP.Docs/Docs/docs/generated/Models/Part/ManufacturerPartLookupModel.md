---
hide:
    - toc
---
Represents a part lookup request sent to the manufacturer's system.
 Tracks who requested it, the part details, order type, and the manufacturer's response.

| Property | Summary |
|----------|---------|
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| UserID <div><strong>``long?``</strong></div> | The ID of the user who initiated the lookup request. |
| UserEmail <div><strong>``string?``</strong></div> | The email address of the user who initiated the lookup request. |
| PartNumber <div><strong>``string``</strong></div> | The part number being looked up at the manufacturer. |
| Quantity <div><strong>``decimal``</strong></div> | The quantity requested for this part. |
| OrderType <div><strong>``ManufacturerOrderType``</strong></div> | The type of manufacturer order (e.g., Stock or Emergency). |
| LogId <div><strong>``string?``</strong></div> | An optional log identifier for tracing this lookup request. |
| Status <div><strong>``ManufacturerPartLookupStatus``</strong></div> | The current status of this manufacturer part lookup request. |
| ManufacturerResult <div><strong>``IEnumerable<KeyValuePair<string, string>>?``</strong></div> | Key-value pairs returned by the manufacturer's system as the lookup result. |
