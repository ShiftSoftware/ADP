---
hide:
    - toc
---
Diagnostic trace of the repair check for one Special Service Campaign (SSC) on one vehicle: every piece of
 evidence the evaluator looked at and what it made of it. Present on `SscDTO.Trace` only when the
 request asked for it via `VehicleLookupRequestOptions.TraceSSCEvaluation`; it lists claim and
 invoice details, so hosts should gate that option behind a permission check.

| Property | Summary |
|----------|---------|
| CampaignLaborCodes <div><strong>``List<string>``</strong></div> | The labor operation codes the campaign itself lists, as fed by the manufacturer (trimmed). |
| InterchangeableLaborCodes <div><strong>``List<SscRepairTraceLaborCodeDTO>``</strong></div> | Additional codes accepted as equivalent to a campaign code through `LookupOptions.SSCInterchangeableLaborCodeGroups`, each with the campaign code it stands for. Empty when no configured group contains any of the campaign's codes. |
| RecordRepairDate <div><strong>``DateTime?``</strong></div> | The repair date carried by the SSC record itself, when the source feed provides one. |
| WarrantyClaims <div><strong>``List<SscRepairTraceWarrantyClaimDTO>``</strong></div> | Every warranty claim on the vehicle, most recently completed first, with how each was judged. |
| ServiceHistoryLaborLinesExamined <div><strong>``int``</strong></div> | How many service-history labor lines the vehicle has in total (all codes, all statuses). |
| ServiceHistoryLaborLines <div><strong>``List<SscRepairTraceLaborLineDTO>``</strong></div> | The service-history labor lines whose code matched a campaign code (directly or through an interchangeable group), most recent first. |
| RepairSource <div><strong>``SscRepairSource``</strong></div> | Which source decided the verdict. |
