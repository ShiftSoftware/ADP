---
hide:
    - toc
---
A code that yielded no milestone, and why.

| Property | Summary |
|----------|---------|
| Code <div><strong>``string``</strong></div> | The code as the source system wrote it. |
| Lines <div><strong>``long``</strong></div> | Labour lines carrying it. |
| Reason <div><strong>``ServiceCodeReadOutcome?``</strong></div> | Why it did not resolve, or null when the configured resolver does not explain itself. Most entries are ordinary unscheduled work; the ones to look at are those a convention claimed and then discarded. |
| Convention <div><strong>``string``</strong></div> | The convention that matched it, when one did and the reading was then discarded. |
| MilestoneInKilometres <div><strong>``long?``</strong></div> | The mileage that was read and rejected, when the reason is `ServiceCodeReadOutcome.ImplausibleMilestone`. Tells a pattern reading the wrong part of the code from bounds set tighter than the deployment schedules. |
