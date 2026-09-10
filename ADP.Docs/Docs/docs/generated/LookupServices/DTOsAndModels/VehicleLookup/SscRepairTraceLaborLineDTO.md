---
hide:
    - toc
---
One service-history labor line whose code is one of the SSC campaign's labor codes (or interchangeable with
 one), as the repair check saw it. Lines with unrelated codes are not listed; only their count is reported.

| Property | Summary |
|----------|---------|
| InvoiceNumber <div><strong>``string``</strong></div> | The invoice the labor line belongs to. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The invoice date. This becomes the SSC repair date when the line is selected. |
| InvoiceStatus <div><strong>``string``</strong></div> | The invoice status as recorded by the dealer system. |
| LaborCode <div><strong>``string``</strong></div> | The labor operation code on the line. |
| CampaignLaborCode <div><strong>``string``</strong></div> | The campaign's own labor code the line's code stands for (equal to `LaborCode` on a direct match). |
| StatusQualifies <div><strong>``bool``</strong></div> | Whether the invoice status counts as repair evidence (X or C). |
| Selected <div><strong>``bool``</strong></div> | Whether this is the line that decided the verdict: the most recent qualifying line, consulted only when no warranty claim was selected. |
