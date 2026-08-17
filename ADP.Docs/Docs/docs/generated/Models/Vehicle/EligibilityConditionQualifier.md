---
hide:
    - toc
---
Decides how the trailing variant token of a milestone package code is treated — the part after
 the milestone itself, which a deployment may use for trim, fuel type or service class.

| Property | Summary |
|----------|---------|
| Selection <div><strong>``EligibilityConditionQualifierSelection``</strong></div> | How the trailing qualifier decides whether a milestone code takes part. |
| Values <div><strong>``IEnumerable<string>``</strong></div> | The qualifiers named by the selection. Required by `EligibilityConditionQualifierSelection.Only` and `EligibilityConditionQualifierSelection.Except`, and must be omitted by every other selection, which fails closed rather than quietly disregarding a list the author wrote. |
