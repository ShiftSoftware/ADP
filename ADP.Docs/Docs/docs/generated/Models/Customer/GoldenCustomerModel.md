---
hide:
    - toc
---
Represents a Golden Customer record — a unified, deduplicated identity that links multiple Dealer Customer records for the same person across different systems.
 The GoldenCustomerID and CustomerID are always the same value on this record.

| Property | Summary |
|----------|---------|
| GoldenCustomerID <div><strong>``string``</strong></div> | The unique Golden Customer identifier. This is the same value as `CustomerID`. |
| CustomerID <div><strong>``string``</strong></div> | The customer identifier. This is the same value as `GoldenCustomerID`. |
| FullName <div><strong>``string``</strong></div> | The survived (golden) full name — the fullest consistent name chain across the linked source records, chosen by the identity-resolution engine's survivorship rules. |
| PhoneNumbers <div><strong>``IEnumerable<string>``</strong></div> | The survived phone number(s) for this identity. |
| City <div><strong>``string``</strong></div> | The survived city / district, when the linked sources carry one. |
| IDNumber <div><strong>``string``</strong></div> | The survived national / social ID number, when any linked source carries one. |
| Email <div><strong>``string``</strong></div> | The survived e-mail address, when any linked source carries one (app registrations and CRM tickets do; dealer DMS extracts typically don't). |
| SourceProfiles <div><strong>``IEnumerable<string>``</strong></div> | Backlinks to the source records this identity unifies, as "sourceSystem\|sourceRecordId" keys. Source records also carry the forward link via their own GoldenCustomerID property. Note: a golden identity is deliberately NOT company-scoped (it may span dealers), so this document carries no CompanyID — it lives at the container's undefined level-1 partition. |
