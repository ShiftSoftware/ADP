---
hide:
    - toc
---
One menu line: the DMS menu code and labour code, plus the priced parts and labour that compose it.

  Those are dealer figures and live on the DMS
 export's own line type; the lookup never even asks the generator for cost. A test asserts this type
 carries no such property, because this is precisely where one would get copied across by accident.

| Property | Summary |
|----------|---------|
| LineKey <div><strong>``string``</strong></div> | Stable identity of this line, independent of language — use it to correlate the same line across language requests. Never key on `Code` for that: it is language-dependent. |
| Code <div><strong>``string``</strong></div> | The generated menu code, as the DMS knows it. |
| LabourCode <div><strong>``string``</strong></div> | The generated labour operation code. |
| Description <div><strong>``string``</strong></div> | The service interval's description for a scheduled line; the item's or group's name for a standalone one. |
| LineType <div><strong>``ServiceMenuLineType``</strong></div> | What produced this line. Serialized as a string, matching `VehicleLookup.VehicleServiceMenuLineDTO.LineType` — the same enum reaching a caller as `"Periodic"` on one endpoint and `0` on another would be a trap. |
| IsStandalone <div><strong>``bool``</strong></div> | Convenience over `LineType`; both standalone shapes report true. |
| ServiceIntervalCode <div><strong>``string``</strong></div> | The service interval's code. Null on standalone lines. |
| ServiceIntervalValueInMeter <div><strong>``int?``</strong></div> | The odometer reading this service is due at, and the sort key for the schedule. Null on standalone lines. DESPITE THE NAME THIS IS IN KILOMETRES, not metres — do not divide by 1000 to render it, or a 20,000 km service is quoted to a customer as 20 km. The name is the source column's (`ServiceInterval.ValueInMeter`), carried through verbatim so the two can be matched up; it is not a unit. The catalogue authors `ValueInMeter = 20000` alongside `FullName = "20,000 KM"`. |
| LabourRate <div><strong>``decimal``</strong></div> | The labour rate charged, per hour — the country rate, or the variant's primary rate. |
| AllowedTime <div><strong>``decimal``</strong></div> | Allowed time in hours. |
| LabourPrice <div><strong>``decimal``</strong></div> | `LabourRate` × `AllowedTime`. |
| Consumable <div><strong>``decimal``</strong></div> | The consumable charge, already scaled by the transfer rate. Always 0 on standalone lines. |
| LabourTotalPrice <div><strong>``decimal``</strong></div> | `LabourPrice` + `Consumable`. |
| Parts <div><strong>``List<ServiceMenuPartDTO>``</strong></div> | The parts on this line, in the order the generator emitted them. |
| PartsTotalPrice <div><strong>``decimal``</strong></div> | Sum of every part's `ServiceMenuPartDTO.TotalPrice`. |
| DiscountPercentage <div><strong>``decimal?``</strong></div> | The variant's discount percentage applied to this line. Null when there is none. |
| DiscountAmount <div><strong>``decimal``</strong></div> | The amount `DiscountPercentage` takes off. 0 when there is no discount. |
| TotalPrice <div><strong>``decimal``</strong></div> | What the customer pays: `LabourTotalPrice` + `PartsTotalPrice`, less the discount. The same arithmetic the DMS export's menu total uses. |
| HasUnpricedParts <div><strong>``bool``</strong></div> | True when at least one part on the line had no price row for the requested country, so its price — and therefore `PartsTotalPrice` and `TotalPrice` — is understated rather than genuinely zero. Lets a UI mark the total as incomplete instead of quoting it. |
