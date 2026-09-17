using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Options passed to the vehicle lookup service to control lookup behavior, language, logging, and consistency level.
/// </summary>
[Docable]
public class VehicleLookupRequestOptions
{
    /// <summary>The language code for localized content (defaults to "en").</summary>
    public string LanguageCode { get; set; } = "en";
    /// <summary>
    /// Whether free service may start while a broker holds the vehicle without an invoice. The lookup a dealer
    /// sees sets this, so a customer is not turned away because the broker has not entered the invoice yet: the
    /// free-service start then comes from the service activation or from the sale that moved the vehicle to the
    /// broker. The warranty still waits for the broker's invoice. Off by default, so a bulk projection leaves
    /// such a vehicle without a free-service start (unless a claim supplies the de facto date).
    /// </summary>
    public bool IgnoreBrokerStock { get; set; }
    /// <summary>
    /// Whether free service is evaluated for financial provisioning instead of for the dealer's lookup. Off by
    /// default: the dealer's lookup starts free service on the end-customer sale only (with the de facto claim
    /// date and an operator's date shift applied), so a vehicle still in dealer stock, a vehicle only a
    /// supply-chain entry has synced for, or a vehicle an un-invoiced broker holds shows no items.
    /// <para>The provisioning view is the distributor's: it books the free-service provision when it invoices a
    /// vehicle. So <see cref="VehicleWarrantyDTO.FreeServiceStartDate"/> is the distributor's own invoice date
    /// (<see cref="VehicleSaleInformation.Distributor"/>), whatever happens to the vehicle afterwards. A dealer's
    /// sale, a broker's invoice, a service activation, a claim or a date shift does not move it. The items then
    /// project from that day: pending while their validity still runs, expired once it has run out, processed
    /// where a claim exists. A vehicle the distributor has not invoiced out projects nothing.</para>
    /// <para>The two views are meant to be read together: the provision booked at the invoice (this view) and
    /// the liability as it actually activated (the dealer's view). The de facto date is still exposed on
    /// <see cref="VehicleWarrantyDTO.DeFactoServiceStartDate"/>. The warranty dates do not move in either view.
    /// </para>
    /// </summary>
    public bool FreeServiceProvisioning { get; set; }
    /// <summary>
    /// Whether to insert an SSC lookup audit log entry. The log is a KPI entry — distributors count SSC lookups
    /// from it — so it is written once per lookup and never for a diagnostic re-read: when
    /// <see cref="TraceSSCEvaluation"/> or <see cref="TraceServiceItemEvaluation"/> is set the flag is ignored,
    /// because a trace request re-reads a lookup that was already logged.
    /// </summary>
    public bool InsertSSCLog { get; set; }
    /// <summary>The SSC log info to record if InsertSSCLog is true.</summary>
    public SSCLogInfo SSCLogInfo { get; set; }
    /// <summary>
    /// Whether to insert a customer vehicle lookup audit log entry. Ignored, like <see cref="InsertSSCLog"/>,
    /// when the request asks for an evaluation trace.
    /// </summary>
    public bool InsertCustomerVehcileLookupLog { get; set; }
    /// <summary>The customer vehicle lookup log info to record.</summary>
    public CustomerVehicleLookupLogInfo CustomerVehicleLookupLogInfo { get; set; }
    /// <summary>The consistency level for service history queries (defaults to Strong).</summary>
    public ConsistencyLevels VehicleServiceHistoryConsistencyLevel { get; set; } = ConsistencyLevels.Strong;
    /// <summary>Whether to look up end customer information for the vehicle.</summary>
    public bool LookupEndCustomer { get; set; }
    /// <summary>Whether to include the legacy paint thickness format in the response.</summary>
    public bool LegacyPaintThickness { get; set; }
    /// <summary>
    /// Whether to generate the Paint Thickness Certificate's signed public URLs
    /// (<c>VehicleLookupDTO.PaintThicknessCertificateUrls</c>) when the certificate is available.
    /// The endpoint decides this per request — typically from a server-side permission check —
    /// so the capability URLs are only produced for callers allowed to print the certificate.
    /// </summary>
    public bool GeneratePaintThicknessCertificateUrls { get; set; }
    /// <summary>Whether to use Katashiki instead of VariantCode for vehicle model lookup.</summary>
    public bool UseKatashikiLookup { get; set; }

    /// <summary>
    /// When true, the service item evaluator records a structured
    /// <see cref="Diagnostics.ServiceItemTrace"/> of every decision (eligibility, expansion,
    /// status, post-processing) and attaches it to <see cref="VehicleLookupDTO.ServiceItemTrace"/>.
    /// Off by default; opt in per request only when debugging. Adds an O(items) walk and
    /// per-item allocations; do not leave on in production hot paths.
    /// </summary>
    public bool TraceServiceItemEvaluation { get; set; }

    /// <summary>
    /// When true, every <see cref="SscDTO"/> in the response carries a <see cref="SscDTO.Trace"/> explaining
    /// how its repair status was decided: the campaign's labor codes and their configured interchangeable
    /// codes, every warranty claim on the vehicle with how it was judged, and the service-history labor lines
    /// that matched. Off by default. The trace lists claim and invoice details, so the endpoint should turn it
    /// on only for callers permitted to see them — typically behind the same gate as
    /// <see cref="TraceServiceItemEvaluation"/>.
    /// </summary>
    public bool TraceSSCEvaluation { get; set; }

    /// <summary>
    /// The model's service menu — whether to include it
    /// (<see cref="VehicleServiceMenuRequestOptions.Include"/>) and how to generate it. Null means no menu,
    /// which is the default. The switch lives beside the settings it governs so the two cannot disagree: a
    /// caller cannot set a country or transfer rate here and have them silently ignored because a separate
    /// flag was missed. See <see cref="VehicleServiceMenuRequestOptions"/>.
    /// </summary>
    public VehicleServiceMenuRequestOptions ServiceMenuOptions { get; set; }

    /// <summary>
    /// The Identity <c>CompanyID</c> of the user/company making the lookup. The authenticated host sets it from
    /// <c>IdentityClaimProvider.GetCompanyID()</c>. Used by the allocation guard
    /// (<c>LookupOptions.RequireAllocationForActivation</c>) to decide whether warranty activation may be offered:
    /// activation is only offered when this company has a vehicle entry for the vehicle. Null for anonymous and bulk
    /// callers (no user context), in which case the activation affordance is suppressed.
    /// </summary>
    public long? RequestingCompanyID { get; set; }
}