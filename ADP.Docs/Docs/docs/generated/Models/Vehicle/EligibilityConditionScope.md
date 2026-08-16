---
hide:
    - toc
---
Defines how a collection-backed condition selects source entries.

| Property | Summary |
|----------|---------|
| Selection <div><strong>``EligibilityConditionSelection``</strong></div> | The collection selection strategy. This is the difference between "is this the case right now" and "has this ever happened", so it decides whether a condition about something the vehicle has done keeps matching once newer entries arrive. |
| Count <div><strong>``int?``</strong></div> | The number of latest entries that take part in the comparison. Required by `EligibilityConditionSelection.Latest` and must be omitted by every other selection, which fails closed rather than quietly disregarding a number the author wrote. |
