---
hide:
    - toc
---
One part on a menu line, priced for the requested country.

 Retail only — there is deliberately no dealer cost here. See `ServiceMenuLineDTO`.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number as authored on the menu item. |
| SortOrder <div><strong>``int``</strong></div> | The authored display order within its menu item. |
| Quantity <div><strong>``decimal``</strong></div> | How many are used by this service. |
| UnitPrice <div><strong>``decimal``</strong></div> | Retail unit price for the requested country. 0 when the part has no price row there. |
| TotalPrice <div><strong>``decimal``</strong></div> | `UnitPrice` × `Quantity`. |
| HasCountryPrice <div><strong>``bool``</strong></div> | False when no price row matched the requested country — the prices above are 0 by fallback rather than because the part is free. Distinguishing the two is the caller's business, so it is surfaced rather than hidden. |
