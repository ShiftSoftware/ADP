---
hide:
    - toc
---
Represents the retail price of a part for a specific selling unit (e.g. each, box, pack).

| Property | Summary |
|----------|---------|
| UnitName <div><strong>``string``</strong></div> | The name of the selling unit (e.g. "each", "box"). |
| Price <div><strong>``decimal?``</strong></div> | The retail price value for this unit. Uses the currency/culture metadata of the parent [PriceDTO](/generated/LookupServices/DTOsAndModels/Part/PriceDTO.html). |
| IsDefault <div><strong>``bool``</strong></div> | Whether this is the default retail unit price. Exactly one entry in the list is the default. |
| FormattedValue <div><strong>``string``</strong></div> | The unit price formatted as a currency string using the parent price's culture. |
