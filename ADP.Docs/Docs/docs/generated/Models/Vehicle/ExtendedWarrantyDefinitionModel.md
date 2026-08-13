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
| ProviderCompanyID <div><strong>``long?``</strong></div> | The provider company's Identity ID. When omitted, the lookup host's configured `LookupOptions.DistributorCompanyID` is used. |
| ActiveFor <div><strong>``int?``</strong></div> | The number of duration units for which the coverage remains valid. |
| ActiveForDurationType <div><strong>``DurationType?``</strong></div> | The duration unit used with `ActiveFor`. |
| EligibilityConditions <div><strong>``IEnumerable<ServiceItemEligibilityConditionModel>``</strong></div> | Declarative predicates that must all match before this coverage is awarded. This uses the same closed condition grammar as service-item eligibility; unsupported conditions fail closed. |
