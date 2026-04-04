---
hide:
    - toc
---

```gherkin
Feature: Vehicle Specification
  Vehicle specifications (engine, transmission, model details) are
  retrieved from the storage service based on the vehicle's variant and brand.

Scenario: Specification retrieved for known vehicle
  Given vehicles in dealer stock:
    | VIN               | VariantCode | BrandID |
    | 1FDKF37GXVEB34368 | VAR-001     | 1       |
  And vehicle model for variant "VAR-001" brand 1:
    | ModelDescription | BodyType | Engine | Transmission |
    | Camry 2024       | Sedan    | 2.5L   | Automatic    |
  When evaluating specification for "1FDKF37GXVEB34368"
  Then the specification model is "Camry 2024"
  And the specification body type is "Sedan"
  And the specification engine is "2.5L"
  And the specification transmission is "Automatic"

Scenario: No specification available returns empty fields
  Given vehicles in dealer stock:
    | VIN               | VariantCode | BrandID |
    | 1FDKF37GXVEB34368 | VAR-999     | 1       |
  When evaluating specification for "1FDKF37GXVEB34368"
  Then the specification model is empty
```
