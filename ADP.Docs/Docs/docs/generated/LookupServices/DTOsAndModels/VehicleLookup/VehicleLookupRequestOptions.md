---
hide:
    - toc
---
Options passed to the vehicle lookup service to control lookup behavior, language, logging, and consistency level.

| Property | Summary |
|----------|---------|
| LanguageCode <div><strong>``string``</strong></div> | The language code for localized content (defaults to "en"). |
| IgnoreBrokerStock <div><strong>``bool``</strong></div> | Whether to skip broker stock lookup for this request. |
| InsertSSCLog <div><strong>``bool``</strong></div> | Whether to insert an SSC lookup audit log entry. |
| SSCLogInfo <div><strong>``SSCLogInfo``</strong></div> | The SSC log info to record if InsertSSCLog is true. |
| InsertCustomerVehcileLookupLog <div><strong>``bool``</strong></div> | Whether to insert a customer vehicle lookup audit log entry. |
| CustomerVehicleLookupLogInfo <div><strong>``CustomerVehicleLookupLogInfo``</strong></div> | The customer vehicle lookup log info to record. |
| VehicleServiceHistoryConsistencyLevel <div><strong>``ConsistencyLevels``</strong></div> | The consistency level for service history queries (defaults to Strong). |
| LookupEndCustomer <div><strong>``bool``</strong></div> | Whether to look up end customer information for the vehicle. |
| LegacyPaintThickness <div><strong>``bool``</strong></div> | Whether to include the legacy paint thickness format in the response. |
| GeneratePaintThicknessCertificateUrls <div><strong>``bool``</strong></div> | Whether to generate the Paint Thickness Certificate's signed public URLs (`VehicleLookupDTO.PaintThicknessCertificateUrls`) when the certificate is available. The endpoint decides this per request — typically from a server-side permission check — so the capability URLs are only produced for callers allowed to print the certificate. |
| UseKatashikiLookup <div><strong>``bool``</strong></div> | Whether to use Katashiki instead of VariantCode for vehicle model lookup. |
| TraceServiceItemEvaluation <div><strong>``bool``</strong></div> | When true, the service item evaluator records a structured [Diagnostics.ServiceItemTrace](/generated/LookupServices/Diagnostics/ServiceItemTrace.html) of every decision (eligibility, expansion, status, post-processing) and attaches it to `VehicleLookupDTO.ServiceItemTrace`. Off by default; opt in per request only when debugging. Adds an O(items) walk and per-item allocations; do not leave on in production hot paths. |
| RequestingCompanyID <div><strong>``long?``</strong></div> | The Identity `CompanyID` of the user/company making the lookup. The authenticated host sets it from `IdentityClaimProvider.GetCompanyID()`. Used by the allocation guard (`LookupOptions.RequireAllocationForActivation`) to decide whether warranty activation may be offered: activation is only offered when this company has a vehicle entry for the vehicle. Null for anonymous and bulk callers (no user context), in which case the activation affordance is suppressed. |
