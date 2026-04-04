---
hide:
    - toc
---

```gherkin
Feature: Vehicle Entry Selection
	When a dealer has multiple entries for the same VIN in their stock,
	the system selects the most relevant vehicle entry based on invoice date.
	Vehicles without an invoice date (still in stock, not yet sold) take priority.

Scenario: Vehicle without invoice date takes priority
	Given vehicles in dealer stock:
		| VIN               | InvoiceDate |
		| 1FDKF37GXVEB34368 |             |
		| 1FDKF37GXVEB34368 | 2024-01-15  |
	When Checking "1FDKF37GXVEB34368"
	Then the selected vehicle has no invoice date

Scenario: Most recent invoice date selected when all have dates
	Given vehicles in dealer stock:
		| VIN               | InvoiceDate |
		| 1FDKF37GXVEB34368 | 2023-06-01  |
		| 1FDKF37GXVEB34368 | 2024-01-15  |
	When Checking "1FDKF37GXVEB34368"
	Then the selected vehicle has invoice date "2024-01-15"

Scenario: Single vehicle returned as-is
	Given vehicles in dealer stock:
		| VIN               | InvoiceDate |
		| 1FDKF37GXVEB34368 | 2024-01-15  |
	When Checking "1FDKF37GXVEB34368"
	Then the selected vehicle has invoice date "2024-01-15"

Scenario: No vehicles returns null
	When Checking "1FDKF37GXVEB34368"
	Then no vehicle is selected
```
