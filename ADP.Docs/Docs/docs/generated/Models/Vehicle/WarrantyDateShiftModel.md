---
hide:
    - toc
---
Represents an adjustment to the warranty end date for a specific vehicle.
 Used to override the calculated warranty expiration date (which is typically derived from the warranty start date plus the brand's warranty period).

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this warranty date shift applies to. |
| NewDate <div><strong>``DateTime``</strong></div> | The new warranty end date for this vehicle. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this date shift record has been deleted. |
