---
hide:
    - toc
---
One menu variant of a model, and the services it offers.

| Property | Summary |
|----------|---------|
| VariantID <div><strong>``long``</strong></div> | The variant's id in the menu catalog. |
| VariantName <div><strong>``string``</strong></div> | The variant's name as authored (e.g. a trim or drivetrain qualifier). |
| BrandID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
| BrandCode <div><strong>``string``</strong></div> | The brand mapping's company code, as the DMS export writes it. Null when the brand has no mapping row. Distinct from the abbreviation embedded in each line's labour code. |
| DiscountPercentage <div><strong>``decimal?``</strong></div> | The discount percentage applied to every line's total. Null when the variant has none. |
| IsFree <div><strong>``bool``</strong></div> | The variant's menu is offered free of charge. |
| PeriodicServices <div><strong>``List<ServiceMenuLineDTO>``</strong></div> | The scheduled services, one per service interval the variant is available for, ordered by distance. An interval whose group carries no labour detail produces no line — matching the export. |
| StandaloneServices <div><strong>``List<ServiceMenuLineDTO>``</strong></div> | The standalone (non-scheduled) services — individual items and item groups that can be sold on their own. Empty when the variant has no standalone items. |
