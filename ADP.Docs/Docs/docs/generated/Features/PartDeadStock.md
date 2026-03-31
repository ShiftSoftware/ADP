---
hide:
    - toc
---

```gherkin
Feature: Part Dead Stock
  Dead stock identification groups parts by company and lists
  per-branch dead stock quantities.

Scenario: Dead stock grouped by company with branch details
  Given company 1 is named "Acme Motors"
  And branch 10 is named "Downtown Branch"
  And branch 20 is named "Airport Branch"
  And dead stock for part "PRT-001":
    | CompanyID | CompanyHashID | BranchID | BranchHashID | AvailableQuantity |
    | 1         | C-HASH-1      | 10       | B-HASH-10    | 5                 |
    | 1         | C-HASH-1      | 20       | B-HASH-20    | 3                 |
  When evaluating dead stock for part "PRT-001"
  Then there are 1 dead stock companies
  And dead stock company "C-HASH-1" is named "Acme Motors"
  And dead stock company "C-HASH-1" has 2 branches
  And dead stock company "C-HASH-1" branch "B-HASH-10" has quantity 5

Scenario: Dead stock from multiple companies
  Given dead stock for part "PRT-001":
    | CompanyID | CompanyHashID | BranchID | BranchHashID | AvailableQuantity |
    | 1         | C-HASH-1      | 10       | B-HASH-10    | 5                 |
    | 2         | C-HASH-2      | 30       | B-HASH-30    | 8                 |
  When evaluating dead stock for part "PRT-001"
  Then there are 2 dead stock companies

Scenario: No dead stock entries
  When evaluating dead stock for part "PRT-001"
  Then there are 0 dead stock companies
```
