Feature: Vehicle Paint Thickness Inspections
  Paint thickness inspection records show measured thickness values
  per panel, along with inspection metadata.

Scenario: Inspection with panels
  Given paint thickness inspections:
    | InspectionDate | Source |
    | 2024-03-15     | Dealer |
  And paint thickness panels for inspection on "2024-03-15":
    | PanelType | PanelSide | PanelPosition | MeasuredThickness |
    | Hood      | Center    | Front         | 120               |
    | Roof      | Left      | Middle        | 95                |
  When evaluating paint thickness with language "en"
  Then there is 1 paint thickness inspection
  And the inspection on "2024-03-15" has 2 panels

Scenario: No inspections returns null
  When evaluating paint thickness with language "en"
  Then there are no paint thickness inspections
