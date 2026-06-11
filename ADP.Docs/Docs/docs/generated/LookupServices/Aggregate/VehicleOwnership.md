---
hide:
    - toc
---
The resolved company/country/region/branch that owns a vehicle for lookup purposes.
 Produced by `VehicleOwnershipEvaluator`: when a service activation exists it is the
 sole owner — completed only by the activating company's own entry, refusing
 (`IncompleteVehicleServiceActivationException`) when the country cannot be resolved;
 without an activation, the selected entry is the source. Passed to the ownership-sensitive
 evaluators (Sale Information, Service Item eligibility) so they all agree on the same
 owner instead of each reading a possibly stale `VehicleEntryModel`.
 Carries numeric IDs only — consumers hash at serialization via the DTO converters, and a
 per-field (ID, HashID) pair resolved from different sources could silently mismatch.

| Property | Summary |
|----------|---------|
| CompanyID <div><strong>``long?``</strong></div> | The owning company. |
| CountryID <div><strong>``long?``</strong></div> | The owning company's country. |
| RegionID <div><strong>``long?``</strong></div> | The owning company's region. |
| BranchID <div><strong>``long?``</strong></div> | The owning branch. |
