---
hide:
    - toc
---
The company-scoped warranty activation state surfaced to the lookup UI for the requesting dealer.
 Distinct from `VehicleWarrantyDTO.ActivationIsRequired`, which is the company-agnostic
 "activation is due" fact used by reporting.

| Value | Summary |
|-------|---------|
| NotRequired | No activation affordance is shown (not due, or suppressed for an anonymous/bulk caller). |
| Required | Activation is due and offered — the vehicle is allocated to the requesting company. |
| BlockedNotAllocated | Activation is due but cannot be offered — the vehicle is not allocated to the requesting company
 (no vehicle entry for it). Surfaced as a non-actionable warning. |
