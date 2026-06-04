---
hide:
    - toc
---
A single panel reading on a Paint Thickness Certificate.
 One row in the certificate's readings table; carries the resolved panel-image URLs for the gallery.

| Property | Summary |
|----------|---------|
| PanelType <div><strong>``VehiclePanelType``</strong></div> | The type of vehicle panel (e.g., Door, Fender, Roof, Hood, Tail Gate). |
| PanelSide <div><strong>``VehiclePanelSide?``</strong></div> | The side of the vehicle this panel is on (Left, Center, Right), if specified. |
| PanelPosition <div><strong>``VehiclePanelPosition?``</strong></div> | The position of the panel on the vehicle (Front, Middle, Rear), if specified. |
| MeasuredThickness <div><strong>``decimal``</strong></div> | The measured paint thickness on this panel, in microns. |
| PanelLabel <div><strong>``string``</strong></div> | A human-readable label for this panel, e.g. "Door (Front Left)" — derived from the panel's
 type/position/side `[Description]` attributes. Convenience for the print template and landing page. |
| Images <div><strong>``List<string>``</strong></div> | Resolved photo URLs of this panel taken during the inspection. |
