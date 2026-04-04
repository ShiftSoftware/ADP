---
hide:
    - toc
---
Represents a vehicle exclusion from free service item campaigns.
 When a VIN is listed here, it is not eligible for any free service items regardless of other criteria.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) that is excluded from free service items. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this exclusion record has been deleted (effectively re-enabling the VIN for free service items). |
