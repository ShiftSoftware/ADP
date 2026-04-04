---
hide:
    - toc
---
Represents a single panel measurement from a paint thickness inspection.

| Property | Summary |
|----------|---------|
| PanelType <div><strong>``VehiclePanelType``</strong></div> | The type of vehicle panel (e.g., Door, Fender, Hood, Roof, Trunk). |
| MeasuredThickness <div><strong>``decimal``</strong></div> | The measured paint thickness on this panel, in microns. |
| PanelSide <div><strong>``VehiclePanelSide?``</strong></div> | The side of the vehicle this panel is on (Left, Center, Right). |
| PanelPosition <div><strong>``VehiclePanelPosition?``</strong></div> | The position of the panel on the vehicle (Front, Middle, Rear). |
| Images <div><strong>``IEnumerable<string>``</strong></div> | Photo URLs of this panel taken during the inspection. |
