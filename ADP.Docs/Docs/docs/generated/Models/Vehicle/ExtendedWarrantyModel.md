---
hide:
    - toc
---
Represents an extended warranty contract purchased for a vehicle beyond the standard manufacturer warranty.
 Extended warranties go through an approval process and can be revoked after issuing.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) that this extended warranty applies to. |
| StartDate <div><strong>``DateTime?``</strong></div> | The start date of the extended warranty coverage period. |
| EndDate <div><strong>``DateTime?``</strong></div> | The end date of the extended warranty coverage period. |
| IsActive <div><strong>``bool``</strong></div> | Extended Warranty Entries may go through a long approval process until considered active. Or they may be revoked after issuing.
 The status is calculated outside and it's simply passed to ADP as a boolean. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this extended warranty record has been deleted. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
