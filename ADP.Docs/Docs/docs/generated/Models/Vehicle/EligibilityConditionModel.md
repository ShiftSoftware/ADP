---
hide:
    - toc
---
A declarative eligibility predicate evaluated against data exposed by the vehicle lookup.

| Property | Summary |
|----------|---------|
| Field <div><strong>``string``</strong></div> | The fully-qualified vehicle lookup field path to evaluate. Supported paths are defined by the lookup evaluator; this model does not imply arbitrary vehicle lookup traversal. |
| Operator <div><strong>``EligibilityConditionOperator``</strong></div> | How the configured values are combined across the scoped field values. |
| ValueMatch <div><strong>``EligibilityConditionValueMatch``</strong></div> | How each configured value is matched against a scoped field value. Defaults to exact matching so catalogs created before this property was added keep their existing behavior. |
| Program <div><strong>``IEnumerable<string>``</strong></div> | The programmes whose codes take part in a milestone comparison, matched against the leading token of the code. Omit it to accept every programme. |
| Qualifier <div><strong>``EligibilityConditionQualifier``</strong></div> | How the trailing variant token of a milestone code is treated. Required by every milestone-based condition, and an authoring mistake on any other, which fails closed. |
| Values <div><strong>``IEnumerable<string>``</strong></div> | The values required by the comparison. |
| Scope <div><strong>``EligibilityConditionScope``</strong></div> | Optional selection scope for collection-based fields. |
| WhenUnmet <div><strong>``EligibilityConditionUnmetBehavior``</strong></div> | What failing this condition means for the item — hidden, locked, or missed. Defaults to hiding, which is what every condition did before this property existed. |
