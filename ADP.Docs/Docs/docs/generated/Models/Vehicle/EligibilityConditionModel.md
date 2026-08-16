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
| Values <div><strong>``IEnumerable<string>``</strong></div> | The values required by the comparison. |
| Scope <div><strong>``EligibilityConditionScope``</strong></div> | Optional selection scope for collection-based fields. |
