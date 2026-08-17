---
hide:
    - toc
---
What the package-code reader made of one code, including why it made nothing of it.

| Property | Summary |
|----------|---------|
| Outcome <div><strong>``ServiceCodeReadOutcome``</strong></div> | What happened. |
| Convention <div><strong>``string``</strong></div> | The convention that matched, by name, or null when none did. Named even when the reading was then discarded: which convention claimed a code is the first thing to know when one of them is drifting. |
| Reading <div><strong>``ServiceMilestoneReading``</strong></div> | The reading, when there is one. Null for every outcome other than `ServiceCodeReadOutcome.Read`. |
| MilestoneInKilometres <div><strong>``long?``</strong></div> | The mileage the convention captured, even when the plausibility guard then rejected it. Null when nothing numeric was captured at all. This is what tells a bad pattern ("it read the model code as a milestone") from bounds set too tight ("it read the milestone, and the deployment schedules services further out than the bounds admit"). |
