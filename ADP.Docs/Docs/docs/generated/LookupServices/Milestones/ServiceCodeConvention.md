---
hide:
    - toc
---
One way this deployment's source system writes a service code, declared as a regular expression
 with named groups.

| Property | Summary |
|----------|---------|
| Name <div><strong>``string``</strong></div> | What this convention is called. Diagnostics only — it names the convention in the coverage audit and in configuration problems, which is how a convention that has stopped matching anything becomes visible. |
| Pattern <div><strong>``string``</strong></div> | The pattern, matched case-insensitively. ADP reads exactly three named groups — `MilestoneGroupName`, `ProgramGroupName` and `QualifierGroupName` — and ignores every other group the pattern declares, so a convention may capture whatever else it needs to describe the shape. |
