---
hide:
    - toc
---
Represents an adjustment to the eligibility start date of free service items for a specific vehicle.
 Used to override the default free service start date (which is typically derived from the warranty activation or invoice date).

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this date shift applies to. |
| NewDate <div><strong>``DateTime``</strong></div> | The new free service eligibility start date for this vehicle. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this date shift record has been deleted. |
