---
hide:
    - toc
---
Represents a part required for a safety recall (SSC) repair.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number required for the recall repair. |
| PartDescription <div><strong>``string``</strong></div> | A description of the part. |
| IsAvailable <div><strong>``bool?``</strong></div> | Whether this part is currently in stock for the requester: `true` = in stock, `false` = not in stock, `null` = availability was not checked (the requester has no Hub stock scope, or the recall is already repaired). The UI renders these three states as a green check, a red cross, and a neutral grey chip respectively. |
