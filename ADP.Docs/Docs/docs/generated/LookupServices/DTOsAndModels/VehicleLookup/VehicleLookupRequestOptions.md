---
hide:
    - toc
---
Options passed to the vehicle lookup service to control lookup behavior, language, logging, and consistency level.

| Property | Summary |
|----------|---------|
| LanguageCode <div><strong>``string``</strong></div> | The language code for localized content (defaults to "en"). |
| IgnoreBrokerStock <div><strong>``bool``</strong></div> | Whether to skip broker stock lookup for this request. |
| InsertSSCLog <div><strong>``bool``</strong></div> | Whether to insert an SSC lookup audit log entry. The log is a KPI entry — distributors count SSC lookups from it — so it is written once per lookup and never for a diagnostic re-read: when `TraceSSCEvaluation` or `TraceServiceItemEvaluation` is set the flag is ignored, because a trace request re-reads a lookup that was already logged. |
| SSCLogInfo <div><strong>``SSCLogInfo``</strong></div> | The SSC log info to record if InsertSSCLog is true. |
| InsertCustomerVehcileLookupLog <div><strong>``bool``</strong></div> | Whether to insert a customer vehicle lookup audit log entry. Ignored, like `InsertSSCLog`, when the request asks for an evaluation trace. |
| CustomerVehicleLookupLogInfo <div><strong>``CustomerVehicleLookupLogInfo``</strong></div> | The customer vehicle lookup log info to record. |
| VehicleServiceHistoryConsistencyLevel <div><strong>``ConsistencyLevels``</strong></div> | The consistency level for service history queries (defaults to Strong). |
| LookupEndCustomer <div><strong>``bool``</strong></div> | Whether to look up end customer information for the vehicle. |
| LegacyPaintThickness <div><strong>``bool``</strong></div> | Whether to include the legacy paint thickness format in the response. |
| GeneratePaintThicknessCertificateUrls <div><strong>``bool``</strong></div> | Whether to generate the Paint Thickness Certificate's signed public URLs (`VehicleLookupDTO.PaintThicknessCertificateUrls`) when the certificate is available. The endpoint decides this per request — typically from a server-side permission check — so the capability URLs are only produced for callers allowed to print the certificate. |
| UseKatashikiLookup <div><strong>``bool``</strong></div> | Whether to use Katashiki instead of VariantCode for vehicle model lookup. |
| TraceServiceItemEvaluation <div><strong>``bool``</strong></div> | When true, the service item evaluator records a structured [Diagnostics.ServiceItemTrace](/generated/LookupServices/Diagnostics/ServiceItemTrace.html) of every decision (eligibility, expansion, status, post-processing) and attaches it to `VehicleLookupDTO.ServiceItemTrace`. Off by default; opt in per request only when debugging. Adds an O(items) walk and per-item allocations; do not leave on in production hot paths. |
| TraceSSCEvaluation <div><strong>``bool``</strong></div> | When true, every [SscDTO](/generated/LookupServices/DTOsAndModels/VehicleLookup/SscDTO.html) in the response carries a `SscDTO.Trace` explaining how its repair status was decided: the campaign's labor codes and their configured interchangeable codes, every warranty claim on the vehicle with how it was judged, and the service-history labor lines that matched. Off by default. The trace lists claim and invoice details, so the endpoint should turn it on only for callers permitted to see them — typically behind the same gate as `TraceServiceItemEvaluation`. |
| ServiceMenuOptions <div><strong>``VehicleServiceMenuRequestOptions``</strong></div> | The model's service menu — whether to include it (`VehicleServiceMenuRequestOptions.Include`) and how to generate it. Null means no menu, which is the default. The switch lives beside the settings it governs so the two cannot disagree: a caller cannot set a country or transfer rate here and have them silently ignored because a separate flag was missed. See [VehicleServiceMenuRequestOptions](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceMenuRequestOptions.html). |
| RequestingCompanyID <div><strong>``long?``</strong></div> | The Identity `CompanyID` of the user/company making the lookup. The authenticated host sets it from `IdentityClaimProvider.GetCompanyID()`. Used by the allocation guard (`LookupOptions.RequireAllocationForActivation`) to decide whether warranty activation may be offered: activation is only offered when this company has a vehicle entry for the vehicle. Null for anonymous and bulk callers (no user context), in which case the activation affordance is suppressed. |
