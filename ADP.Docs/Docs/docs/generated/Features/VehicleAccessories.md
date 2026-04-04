---
hide:
    - toc
---

```gherkin
Feature: Vehicle Accessories
  Vehicle accessories are listed with their part numbers, descriptions,
  and images. Image URLs are resolved through a configurable resolver.

Scenario: Accessories listed with resolved image URLs
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
    | ACC-002    | Roof rack       | img002.jpg |
  And the accessory image resolver maps "img001.jpg" to "https://cdn.example.com/img001.jpg"
  And the accessory image resolver maps "img002.jpg" to "https://cdn.example.com/img002.jpg"
  When evaluating accessories with language "en"
  Then there are 2 accessories
  And accessory "ACC-001" has image "https://cdn.example.com/img001.jpg"
  And accessory "ACC-002" has image "https://cdn.example.com/img002.jpg"

Scenario: Accessories without image resolver return raw image value
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has image "img001.jpg"

Scenario: No accessories returns empty list
  When evaluating accessories with language "en"
  Then there are 0 accessories
```
