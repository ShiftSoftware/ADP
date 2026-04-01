---
hide:
    - toc
---
Represents a paint thickness inspection performed on a vehicle.
 Paint thickness measurements across multiple panels help detect prior bodywork, repainting, or accident damage.

| Property | Summary |
|----------|---------|
| ModelYear <div><strong>``string``</strong></div> | The model year of the vehicle at the time of inspection. |
| ModelDescription <div><strong>``string``</strong></div> | A description of the vehicle model at the time of inspection. |
| ColorCode <div><strong>``string``</strong></div> | The exterior color code of the vehicle at the time of inspection. |
| InspectionDate <div><strong>``DateTime?``</strong></div> | The date the paint thickness inspection was performed. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) of the inspected vehicle. |
| Panels <div><strong>``IEnumerable<PaintThicknessInspectionPanelModel>``</strong></div> | The individual [panel measurements](/generated/Models/Vehicle/PaintThicknessInspectionPanelModel.html) taken during the inspection. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| Source <div><strong>``string``</strong></div> | The source system or process that performed this inspection. |
