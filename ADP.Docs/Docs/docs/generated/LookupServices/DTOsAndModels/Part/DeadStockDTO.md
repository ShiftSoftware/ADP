---
hide:
    - toc
---
Represents dead stock information for a part at a specific company, grouped by branch.

| Property | Summary |
|----------|---------|
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| CompanyName <div><strong>``string``</strong></div> | The resolved company name. |
| BranchDeadStock <div><strong>``IEnumerable<BranchDeadStockDTO>``</strong></div> | Dead stock quantities broken down by branch. |
