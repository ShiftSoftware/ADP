---
hide:
    - toc
---
How this deployment's service-history codes name a scheduled service, and what counts as a
 believable milestone once one is read.

| Property | Summary |
|----------|---------|
| PackageCodePattern <div><strong>``string``</strong></div> | The pattern that reads a milestone out of a package code, capturing the number of thousands in its first group. Matched case-insensitively. A code holding more than one match is read as holding none, because which of them is the milestone would be a guess. |
| MinimumInKilometres <div><strong>``long``</strong></div> | The smallest believable milestone, in kilometres. |
| MaximumInKilometres <div><strong>``long``</strong></div> | The largest believable milestone, in kilometres. Set clear of the largest milestone the deployment actually schedules rather than exactly on it, so a genuine service added later is not silently discarded by a bound nobody remembers setting. |
| StepInKilometres <div><strong>``long``</strong></div> | The interval milestones are scheduled at, in kilometres. A reading that is not a whole number of these is not a milestone. Set to 0 to accept any spacing. |
| Resolver <div><strong>``IServiceMilestoneResolver``</strong></div> | Replaces the built-in package-code reader entirely — the seam a host uses to supply milestones from a source that states them rather than implies them. When unset, the settings above build the built-in reader. |
