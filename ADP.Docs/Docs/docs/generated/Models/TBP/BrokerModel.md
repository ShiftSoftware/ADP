---
hide:
    - toc
---
Represents a broker entity linked to a company in the Identity System.
 Contains the broker's account period, account numbers, and deletion status.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this broker. |
| Name <div><strong>``string``</strong></div> | The broker's name. |
| AccountStartDate <div><strong>``DateTime?``</strong></div> | The date from which the broker's account became active. |
| TerminationDate <div><strong>``DateTime?``</strong></div> | The date the broker's account was terminated. Null if still active. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| AccountNumbers <div><strong>``IEnumerable<string>``</strong></div> | The list of account numbers assigned to this broker. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this broker has been deleted. |
