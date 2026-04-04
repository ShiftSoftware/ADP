---
hide:
    - toc
---

```gherkin
Feature: Part Stock Availability
  Stock availability checks how many units of a part are available
  across locations. A quantity threshold can filter out requests above
  a certain level. Quantity is only shown when ShowPartLookupStockQauntity
  is enabled.

Scenario: Stock available at multiple locations
  Given LookupOptions distributor stock threshold is 100
  And LookupOptions show stock quantity is enabled
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 50                | 1         |
    | LOC-002  | 10                | 1         |
  When evaluating stock for part "PRT-001" with quantity 5
  Then there are 2 stock entries
  And stock entry "LOC-001" has result "Available"
  And stock entry "LOC-001" has available quantity 50
  And stock entry "LOC-002" has result "Available"

Scenario: Partially available stock
  Given LookupOptions distributor stock threshold is 100
  And LookupOptions show stock quantity is enabled
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 3                 | 1         |
  When evaluating stock for part "PRT-001" with quantity 5
  Then there are 1 stock entries
  And stock entry "LOC-001" has result "PartiallyAvailable"
  And stock entry "LOC-001" has available quantity 3

Scenario: Not available when quantity is zero or negative
  Given LookupOptions distributor stock threshold is 100
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 0                 | 1         |
  When evaluating stock for part "PRT-001" with quantity 5
  Then stock entry "LOC-001" has result "NotAvailable"

Scenario: Quantity not within lookup threshold
  Given LookupOptions distributor stock threshold is 5
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 50                | 1         |
  When evaluating stock for part "PRT-001" with quantity 10
  Then stock entry "LOC-001" has result "QuantityNotWithinLookupThreshold"

Scenario: Lookup skipped when quantity is null
  Given stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 50                | 1         |
  When evaluating stock for part "PRT-001" without quantity
  Then stock entry "LOC-001" has result "LookupIsSkipped"

Scenario: Quantity hidden when ShowPartLookupStockQauntity is disabled
  Given LookupOptions distributor stock threshold is 100
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 50                | 1         |
  When evaluating stock for part "PRT-001" with quantity 5
  Then stock entry "LOC-001" has result "Available"
  And stock entry "LOC-001" has no available quantity

Scenario: Location name resolved
  Given LookupOptions distributor stock threshold is 100
  And location "LOC-001" is named "Main Warehouse"
  And stock for part "PRT-001":
    | Location | AvailableQuantity | CompanyID |
    | LOC-001  | 50                | 1         |
  When evaluating stock for part "PRT-001" with quantity 5
  Then stock entry "LOC-001" has location name "Main Warehouse"

Scenario: No stock entries
  When evaluating stock for part "PRT-001" with quantity 5
  Then there are 0 stock entries
```
