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
| Parts <div><strong>``IEnumerable<SSCPartDTO>``</strong></div> | The [parts](/generated/LookupServices/DTOsAndModels/VehicleLookup/SSCPartDTO.html) required for the recall repair. |
