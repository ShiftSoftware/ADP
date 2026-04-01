---
hide:
    - toc
---
Represents a paint thickness inspection record for a vehicle, as returned by the vehicle lookup.

| Property | Summary |
|----------|---------|
| Source <div><strong>``string``</strong></div> | The source system or process that performed this inspection. |
| InspectionDate <div><strong>``DateTime?``</strong></div> | The date the inspection was performed. |
| Panels <div><strong>``IEnumerable<PaintThicknessInspectionPanelDTO>``</strong></div> | The individual [panel measurements](/generated/LookupServices/DTOsAndModels/VehicleLookup/PaintThicknessInspectionPanelDTO.html) taken during the inspection. |
