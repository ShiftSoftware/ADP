---
hide:
    - toc
---
How this deployment's service-history codes name a scheduled service, and what counts as a
 believable milestone once one is read.

| Property | Summary |
|----------|---------|
| Conventions <div><strong>``IList<ServiceCodeConvention>``</strong></div> | The ways this deployment's source system writes a service code, in the order they are tried; the first whose pattern matches decides the reading. Empty by design — a deployment that declares none reads no milestones at all, and says so as a distinct state rather than reporting every vehicle as having no service history. |
| MinimumInKilometres <div><strong>``long``</strong></div> | The smallest believable milestone, in kilometres. |
| MaximumInKilometres <div><strong>``long``</strong></div> | The largest believable milestone, in kilometres. Set clear of the largest milestone the deployment actually schedules rather than exactly on it, so a genuine service added later is not silently discarded by a bound nobody remembers setting. |
| StepInKilometres <div><strong>``long``</strong></div> | The interval milestones are scheduled at, in kilometres. A reading that is not a whole number of these is not a milestone. Set to 0 to accept any spacing. |
| Resolver <div><strong>``IServiceMilestoneResolver``</strong></div> | Replaces the built-in package-code reader entirely — the seam a host uses to supply milestones from a source that states them rather than implies them, and the seam that lets a network whose codes are not regex-tractable be fixed without an ADP release. When unset, the settings above build the built-in reader. |
