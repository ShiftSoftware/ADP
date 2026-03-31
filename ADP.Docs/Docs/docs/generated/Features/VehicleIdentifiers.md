---
hide:
    - toc
---

```gherkin
Feature: Vehicle Identifiers
	The vehicle identifier provides key identification fields (VIN, variant,
	color codes, brand) extracted from the vehicle entry record.

Scenario: Identifiers populated from vehicle entry
	Given vehicles in dealer stock:
		| VIN               | VariantCode | Katashiki | ExteriorColorCode | InteriorColorCode | BrandID |
		| 1FDKF37GXVEB34368 | VAR001      | KAT-123   | WHT               | BLK               | 1       |
	When Checking "1FDKF37GXVEB34368"
	Then the vehicle identifiers are:
		| Field     | Value             |
		| VIN       | 1FDKF37GXVEB34368 |
		| Variant   | VAR001            |
		| Katashiki | KAT-123           |
		| Color     | WHT               |
		| Trim      | BLK               |
		| BrandID   | 1                 |

Scenario: No vehicle entry returns VIN only
	When Checking "1FDKF37GXVEB34368"
	Then the vehicle identifiers are:
		| Field     | Value             |
		| VIN       | 1FDKF37GXVEB34368 |
		| Variant   |                   |
		| Katashiki |                   |
		| Color     |                   |
		| Trim      |                   |
		| BrandID   |                   |
```
