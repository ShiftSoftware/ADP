---
hide:
    - toc
---
Represents a single panel measurement from a Paint Thickness Inspection.
 Each panel is identified by its type, side, and position on the vehicle, with a measured thickness value.

| Property | Summary |
|----------|---------|
| PanelType <div><strong>``VehiclePanelType``</strong></div> | The type of vehicle panel (e.g., Door, Fender, Hood, Roof, Trunk). |
| MeasuredThickness <div><strong>``decimal``</strong></div> | The measured paint thickness on this panel, in microns. |
| PanelSide <div><strong>``VehiclePanelSide?``</strong></div> | The side of the vehicle this panel is on (Left, Center, or Right). |
| PanelPosition <div><strong>``VehiclePanelPosition?``</strong></div> | The position of the panel on the vehicle (Front, Middle, or Rear). |
| Images <div><strong>``IEnumerable<string>``</strong></div> | Photo URLs of this panel taken during the inspection. |
