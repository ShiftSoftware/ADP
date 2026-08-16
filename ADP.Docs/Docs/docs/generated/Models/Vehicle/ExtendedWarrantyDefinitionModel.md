---
hide:
    - toc
---
Defines an extended-warranty coverage that can be awarded from declarative vehicle lookup
 eligibility conditions. The condition contract is shared with service-item eligibility so
 hosts can use the same external JSON grammar and matching semantics. Qualifying coverage
 begins at the end of the vehicle's standard warranty and lasts for the configured duration.

| Property | Summary |
|----------|---------|
| ID <div><strong>``string``</strong></div> | A stable identifier for this extended-warranty definition. |
| Name <div><strong>``string``</strong></div> | The name shown for this coverage. The identifier is not a display string, so when this is omitted consumers fall back to their own generic "extended warranty" wording. |
| ProviderCompanyID <div><strong>``long?``</strong></div> | The provider company's Identity ID. When omitted, the lookup host's configured `LookupOptions.DistributorCompanyID` is used. |
| ActiveFor <div><strong>``int?``</strong></div> | The number of duration units for which the coverage remains valid. |
| ActiveForDurationType <div><strong>``DurationType?``</strong></div> | The duration unit used with `ActiveFor`. |
| EligibilityConditions <div><strong>``IEnumerable<EligibilityConditionModel>``</strong></div> | Declarative predicates that must all match before this coverage is awarded. This is the same closed condition grammar service items are gated by; unsupported conditions fail closed. |
