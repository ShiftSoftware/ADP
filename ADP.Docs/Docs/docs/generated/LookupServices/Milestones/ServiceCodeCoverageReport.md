---
hide:
    - toc
---
What this deployment's conventions make of a corpus of service codes: how much of it reads, what
 it reads as, and what is left over.

| Property | Summary |
|----------|---------|
| CanRead <div><strong>``bool``</strong></div> | Whether the reader can produce a milestone at all. False means — every code below is unresolved for that one reason, and the corpus says nothing about the deployment's codes. |
| Problems <div><strong>``IReadOnlyList<ServiceMilestoneConfigurationProblem>``</strong></div> | Settings that could not be used, with the reason. Empty when everything configured compiled. |
| Codes <div><strong>``long``</strong></div> | Distinct codes in the corpus. |
| Lines <div><strong>``long``</strong></div> | Labour lines the corpus accounts for. |
| ResolvedCodes <div><strong>``long``</strong></div> | Distinct codes that yielded a milestone. |
| ResolvedLines <div><strong>``long``</strong></div> | Labour lines carrying a code that yielded a milestone. |
| LineCoverage <div><strong>``double``</strong></div> | Resolved lines as a fraction of all lines, 0 to 1. Weighted by volume rather than by distinct code, because one code on a hundred thousand invoices matters more than a hundred codes on one each — and the reverse ranking is how a reader can look healthy while missing the work customers actually have done. |
| Programs <div><strong>``IReadOnlyList<ServiceCodeCoverageGroup>``</strong></div> | Volume by programme. Reading this is how a mis-ordered alternation is caught: a programme the deployment knows it books work under, showing zero lines, is a pattern fault rather than a fact about the business. |
| Qualifiers <div><strong>``IReadOnlyList<ServiceCodeCoverageGroup>``</strong></div> | Volume by qualifier, the unnamed group being codes that carry none. The distribution a condition's `Qualifier` setting is calibrated against — deciding which variants count from the shape of the catalog rather than from these volumes is how a rule comes to describe a small minority of the work it was meant to cover. |
| Conventions <div><strong>``IReadOnlyList<ServiceCodeCoverageGroup>``</strong></div> | Volume by convention, in the order they are tried, including conventions that matched nothing. A convention at zero has either been superseded or been shadowed by one above it. |
| TopUnresolved <div><strong>``IReadOnlyList<UnresolvedServiceCode>``</strong></div> | The codes that did not resolve, heaviest first, capped at the requested limit. Where drift shows up first: a shape that used to read, or has never read, sitting near the top. |
