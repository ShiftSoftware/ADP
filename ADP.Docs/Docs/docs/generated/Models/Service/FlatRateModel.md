---
hide:
    - toc
---
Represents a flat-rate labor time entry used to look up standard repair times.
 Each entry maps a labor code and vehicle variant (identified by WMI and VDS from the VIN) to a set of time allowances.

| Property | Summary |
|----------|---------|
| LaborCode <div><strong>``string``</strong></div> | The labor operation code identifying the type of work. |
| VDS <div><strong>``string``</strong></div> | The Vehicle Descriptor Section (VDS) from the VIN, identifying the vehicle model/variant. |
| Times <div><strong>``Dictionary<string, decimal?>``</strong></div> | A dictionary of time allowances keyed by time category. Values are in hours. |
| WMI <div><strong>``string``</strong></div> | The World Manufacturer Identifier (WMI) from the VIN, identifying the vehicle manufacturer. |
| BrandHashID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
