---
hide:
    - toc
---
Represents the retail price of a part for a specific selling unit (e.g. each, box, pack).

| Property | Summary |
|----------|---------|
| UnitName <div><strong>``string``</strong></div> | The name of the selling unit (e.g. "each", "box"). Must be unique within the unit price list. |
| Price <div><strong>``decimal?``</strong></div> | The retail price of the part for this unit. |
| IsDefault <div><strong>``bool``</strong></div> | Indicates whether this unit price is the default retail unit price.
 Only one unit price within the list may be marked as the default. |
