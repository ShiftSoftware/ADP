---
hide:
    - toc
---
Represents a dead stock part held by a specific company/branch.
 Dead stock parts are items that have not been sold for a prolonged period and are considered slow-moving or obsolete inventory.

| Property | Summary |
|----------|---------|
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| PartNumber <div><strong>``string``</strong></div> | Each part has a unique part number that is used to identify it in the catalog and other related documents/systems. |
| GenuinePartNumber <div><strong>``bool``</strong></div> | Indicates whether the part is a Genuine Part Number |
| AvailableQuantity <div><strong>``decimal``</strong></div> | The current AvailableQuantity of the part in the stock. |
