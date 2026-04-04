---
hide:
    - toc
---
Request DTO for initiating a part lookup against the manufacturer's system.

| Property | Summary |
|----------|---------|
| PartNumber <div><strong>``string``</strong></div> | The part number to look up. |
| Quantity <div><strong>``decimal``</strong></div> | The quantity requested. |
| OrderType <div><strong>``ManufacturerOrderType``</strong></div> | The order type (e.g., Sea or Airplane shipping). |
| LogId <div><strong>``string?``</strong></div> | An optional log identifier for tracing. |
