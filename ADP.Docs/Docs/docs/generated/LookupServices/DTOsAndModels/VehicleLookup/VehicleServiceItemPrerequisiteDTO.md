---
hide:
    - toc
---
One service that has to have happened before an item unlocks.

| Property | Summary |
|----------|---------|
| Mileage <div><strong>``long``</strong></div> | The scheduled service, in kilometres. |
| Label <div><strong>``string``</strong></div> | The mileage written the way the milestone itself is written — "45K" for 45,000. A plain rendering of `Mileage`, not a name. |
| Satisfied <div><strong>``bool``</strong></div> | Whether the vehicle's service history records this service. |
| SatisfiedOn <div><strong>``DateTime?``</strong></div> | When it was first recorded, or null when it has not been. The earliest invoice date, so a service performed twice reports when the prerequisite was met rather than when it was repeated. |
