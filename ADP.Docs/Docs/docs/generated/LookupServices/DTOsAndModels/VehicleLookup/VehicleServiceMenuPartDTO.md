---
hide:
    - toc
---
One part on a vehicle service-menu line, priced for the country the menu was generated for.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number as authored on the menu item. |
| SortOrder <div><strong>``int``</strong></div> | The authored display order within its menu item. |
| Quantity <div><strong>``decimal``</strong></div> | How many are used by this service. |
| UnitPrice <div><strong>``decimal``</strong></div> | Retail unit price for the country the menu was generated for. 0 when there is no price row. |
| TotalPrice <div><strong>``decimal``</strong></div> | `UnitPrice` × `Quantity`. |
| HasCountryPrice <div><strong>``bool``</strong></div> | False when no price row matched the country — the prices above are 0 by fallback rather than because the part is free. A UI quoting a total needs to be able to tell those apart, so it is surfaced. |
