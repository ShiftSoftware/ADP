---
hide:
    - toc
---
Represents a Special Service Campaign (SSC) / safety recall affecting a vehicle.
 Includes the recall code, description, required labor and parts, and whether the repair has been completed.

| Property | Summary |
|----------|---------|
| SSCCode <div><strong>``string``</strong></div> | The unique code identifying the recall campaign. |
| Description <div><strong>``string``</strong></div> | A description of the recall and the issue being addressed. |
| Labors <div><strong>``IEnumerable<SSCLaborDTO>``</strong></div> | The [labor operations](/generated/LookupServices/DTOsAndModels/VehicleLookup/SSCLaborDTO.html) required for the recall repair. |
| Repaired <div><strong>``bool``</strong></div> | Whether the recall repair has been completed for this vehicle. |
| RepairDate <div><strong>``DateTime?``</strong></div> | The date the recall repair was completed. Null if not yet repaired. |
| RepairSource <div><strong>``SscRepairSource``</strong></div> | Which evidence decided `Repaired`: the SSC record's own repair date, a qualifying warranty claim, or a finished service-history labor line — or `None` when the campaign is still open. |
| RepairCompanyID <div><strong>``string?``</strong></div> | The Company Hash ID of the dealer that carried out the repair: the company on the warranty claim or the labor line that decided `Repaired`. Null when the SSC record's own repair date decided it, and while the campaign is still open. |
| RepairCompanyName <div><strong>``string?``</strong></div> | The localized name of the dealer that carried out the repair. |
| RepairBranchID <div><strong>``string?``</strong></div> | The Branch Hash ID of the dealer branch that carried out the repair, from the same evidence as `RepairCompanyID`. Null when that evidence names no branch. |
| RepairBranchName <div><strong>``string?``</strong></div> | The localized name of the dealer branch that carried out the repair. |
| Parts <div><strong>``IEnumerable<SSCPartDTO>``</strong></div> | The [parts](/generated/LookupServices/DTOsAndModels/VehicleLookup/SSCPartDTO.html) required for the recall repair. |
| Trace <div><strong>``SscRepairTraceDTO?``</strong></div> | Diagnostic trace of how `Repaired` was decided. Null unless the request asked for it via `VehicleLookupRequestOptions.TraceSSCEvaluation`, and omitted from the JSON when null. |
