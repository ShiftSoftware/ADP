---
hide:
    - toc
---
What one piece of service history says about the scheduled service it recorded: which milestone
 was reached, under which programme, and under which variant qualifier.

| Property | Summary |
|----------|---------|
| Milestone <div><strong>``long``</strong></div> | The milestone reached, in kilometres. |
| Program <div><strong>``string``</strong></div> | The programme the service was booked under, or null when the source names none. Compared with `EligibilityConditionModel.Program`. |
| Qualifier <div><strong>``string``</strong></div> | The variant qualifier carried alongside the milestone, or null when the source carries none. Judged by `EligibilityConditionModel.Qualifier`. Null and empty mean the same thing here — a code with nothing after its milestone — and a resolver should report null for both. |
