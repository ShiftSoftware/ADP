---
hide:
    - toc
---

```gherkin
Feature: Part Pricing
  Part pricing includes the distributor purchase price and per-country/region
  retail prices. The PartPriceEvaluator builds up pricing data from CatalogParts
  but only returns results through a configured PartLookupPriceResolver.

Scenario: Part with distributor price and region prices via pass-through resolver
  Given catalog part "PRT-001" with distributor price 25.50
  And catalog part "PRT-001" has country 1 with region prices:
    | RegionID | RetailPrice | PurchasePrice | WarrantyPrice |
    | 10       | 35.00       | 28.00         | 20.00         |
    | 20       | 30.00       | 24.00         | 18.00         |
  And country 1 is named "United States"
  And region 10 is named "East Coast"
  And region 20 is named "West Coast"
  And the part price resolver passes through
  When evaluating price for part "PRT-001"
  Then the distributor price is 25.50
  And there are 2 price entries
  And price entry for country "1" region "10" has retail price 35.00
  And price entry for country "1" region "10" has country name "United States"
  And price entry for country "1" region "10" has region name "East Coast"

Scenario: Part with multiple countries and regions
  Given catalog part "PRT-002" with distributor price 50.00
  And catalog part "PRT-002" has country 1 with region prices:
    | RegionID | RetailPrice | PurchasePrice | WarrantyPrice |
    | 10       | 70.00       | 55.00         | 40.00         |
  And catalog part "PRT-002" has country 2 with region prices:
    | RegionID | RetailPrice | PurchasePrice | WarrantyPrice |
    | 30       | 65.00       | 52.00         | 38.00         |
    | 40       | 68.00       | 54.00         | 39.00         |
  And the part price resolver passes through
  When evaluating price for part "PRT-002"
  Then the distributor price is 50.00
  And there are 3 price entries

Scenario: Without resolver returns empty result
  Given catalog part "PRT-001" with distributor price 25.50
  And catalog part "PRT-001" has country 1 with region prices:
    | RegionID | RetailPrice | PurchasePrice | WarrantyPrice |
    | 10       | 35.00       | 28.00         | 20.00         |
  When evaluating price for part "PRT-001"
  Then the distributor price is empty
  And there are 0 price entries

Scenario: Part with no catalog data
  Given the part price resolver passes through
  When evaluating price for part "PRT-NONE"
  Then the distributor price is empty
  And there are 0 price entries
```
