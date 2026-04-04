---
hide:
    - toc
---
Represents a labor line item within a Warranty Claim.
 Each line captures a specific labor operation performed during the warranty repair.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this labor line. |
| PayCode <div><strong>``string``</strong></div> | The pay code indicating the payment type for this labor operation. |
| MainOperation <div><strong>``bool``</strong></div> | Indicates whether this is the main (primary) operation on the warranty claim. |
| LaborCode <div><strong>``string``</strong></div> | The labor operation code identifying the type of work performed. |
| Hour <div><strong>``decimal``</strong></div> | The number of labor hours claimed by the dealer for this operation. |
| DistributorHour <div><strong>``decimal?``</strong></div> | The number of labor hours approved by the distributor. May differ from the dealer-claimed hours. |
