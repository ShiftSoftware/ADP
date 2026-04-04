---
hide:
    - toc
---
Represents a flat-rate labor time lookup result, as returned by the service lookup.

| Property | Summary |
|----------|---------|
| LaborCode <div><strong>``string``</strong></div> | The labor operation code. |
| VDS <div><strong>``string``</strong></div> | The Vehicle Descriptor Section (VDS) from the VIN. |
| Times <div><strong>``Dictionary<string, decimal?>``</strong></div> | Time allowances keyed by category, in hours. |
| WMI <div><strong>``string``</strong></div> | The World Manufacturer Identifier (WMI) from the VIN. |
| BrandID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
