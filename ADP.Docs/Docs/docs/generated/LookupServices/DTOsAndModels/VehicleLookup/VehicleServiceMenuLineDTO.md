---
hide:
    - toc
---
One service on the vehicle's menu — the DMS menu code and labour code, and the priced parts and labour
 that compose them.

| Property | Summary |
|----------|---------|
| VariantID <div><strong>``long``</strong></div> | The menu variant this service belongs to, in the menus catalog. |
| VariantName <div><strong>``string``</strong></div> | The variant's name as authored (a trim or drivetrain qualifier). A model usually has one variant; when it has several, this is what a UI groups or filters by. |
| IsFree <div><strong>``bool``</strong></div> | The variant's menu is offered free of charge. A variant-level flag travelling on the line, like `VariantName` — every line of a free variant carries it. |
| LineKey <div><strong>``string``</strong></div> | Stable identity of this line, independent of language — use it to correlate the same service across language requests. Never key on `Code` for that: it is language-dependent by construction. |
| Code <div><strong>``string``</strong></div> | The generated menu code, as the DMS knows it. |
| LabourCode <div><strong>``string``</strong></div> | The generated labour operation code. |
| Description <div><strong>``string``</strong></div> | The service interval's description for a scheduled service; the item's or group's name for a standalone one. |
| LineType <div><strong>``ServiceMenuLineType``</strong></div> | What produced this line. Serialized as a string so the generated TypeScript union is honest about the wire format. |
| IsStandalone <div><strong>``bool``</strong></div> | Convenience over `LineType`; both standalone shapes report true. |
| ServiceIntervalCode <div><strong>``string``</strong></div> | The service interval's code. Null on standalone services. |
| ServiceIntervalValueInMeter <div><strong>``int?``</strong></div> | The odometer reading this service is due at, and the axis a schedule is read along. Null on standalone services. DESPITE THE NAME THIS IS IN KILOMETRES — render it as it is; dividing by 1000 quotes a 20,000 km service as 20 km. It carries `ServiceInterval.ValueInMeter` verbatim, and the catalogue authors `20000` there for the service it also names "20,000 KM". |
| LabourRate <div><strong>``decimal``</strong></div> | The labour rate charged, per hour — the country rate, or the variant's primary rate. |
| AllowedTime <div><strong>``decimal``</strong></div> | Allowed time in hours. |
| LabourPrice <div><strong>``decimal``</strong></div> | `LabourRate` × `AllowedTime`. |
| Consumable <div><strong>``decimal``</strong></div> | The consumable charge, already scaled by the transfer rate. Always 0 on standalone services. |
| LabourTotalPrice <div><strong>``decimal``</strong></div> | `LabourPrice` + `Consumable`. |
| Parts <div><strong>``List<VehicleServiceMenuPartDTO>``</strong></div> | The parts this service uses, in the order the generator emitted them. |
| PartsTotalPrice <div><strong>``decimal``</strong></div> | Sum of every part's `VehicleServiceMenuPartDTO.TotalPrice`. |
| DiscountPercentage <div><strong>``decimal?``</strong></div> | The variant's discount percentage applied to this service. Null when there is none. |
| DiscountAmount <div><strong>``decimal``</strong></div> | The amount `DiscountPercentage` takes off. 0 when there is no discount. |
| TotalPrice <div><strong>``decimal``</strong></div> | What the customer pays: `LabourTotalPrice` + `PartsTotalPrice`, less the discount. The same arithmetic the DMS export's menu total uses. |
| HasUnpricedParts <div><strong>``bool``</strong></div> | True when at least one part had no price row for the country, so its price — and therefore `PartsTotalPrice` and `TotalPrice` — is understated rather than genuinely zero. Mark the total as incomplete instead of quoting it. |
