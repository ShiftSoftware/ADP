using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.WarrantyClaims.Web.Extensions;

public class WarrantyClaimsWebOptions
{
    /// <summary>
    /// TypeAuth node gating the WarrantyClaim pages (List/Form). Consumer-supplied so existing grants
    /// keep resolving (decision D9) — e.g. the original host application passes
    /// its existing <c>WarrantySystem.WarrantyClaim</c> node. Null → the pages are not gated.
    /// </summary>
    public ReadWriteDeleteAction? WarrantyClaimAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the WarrantyCertificate pages + the certify shortcut on the claim list
    /// (consumer-supplied, per D9 — the original host application passes <c>WarrantySystem.Certificates</c>). Null → not gated.
    /// </summary>
    public ReadWriteDeleteAction? CertificatesAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the WarrantyInvoice pages (consumer-supplied, per D9 — the original host application passes
    /// <c>WarrantySystem.Invoices</c>). Null → not gated.
    /// </summary>
    public ReadWriteDeleteAction? InvoicesAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the ManufacturerSettlmentSheet pages (consumer-supplied, per D9 — the original host application passes
    /// <c>WarrantySystem.ManufacturerSettlments</c>). Null → not gated.
    /// </summary>
    public ReadWriteDeleteAction? ManufacturerSettlmentsAction { get; set; }

    /// <summary>
    /// TypeAuth node (a BooleanAction) that allows overwriting the delivery date from the claim list
    /// (consumer-supplied — the original host application passes <c>WarrantySystem.DeliveryDateOverwrite</c>). Null → never allowed.
    /// </summary>
    public BooleanAction? DeliveryDateOverwriteAction { get; set; }

    /// <summary>
    /// TypeAuth read node gating the Financial list page (consumer-supplied, per D9 — the original
    /// host application passes <c>WarrantySystem.WarrantyClaimFinancial</c>). Null → not gated.
    /// </summary>
    public ReadAction? WarrantyClaimFinancialAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the AdditionalLaborOperationCode pages (consumer-supplied, per D9 — the
    /// original host application passes <c>WarrantySystem.AdditionalLaborOperationCodes</c>). Null → not gated.
    /// </summary>
    public ReadWriteDeleteAction? AdditionalLaborOperationCodesAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the WarrantyRates admin pages (Phase 3 Slice 3.6, D24 — the original
    /// host's Setting pages moved into the module; those pages passed a null TypeAuth action, so the
    /// host leaves this null too). Null → not gated (authentication-only, like every sibling page).
    /// </summary>
    public ReadWriteDeleteAction? WarrantyRatesAction { get; set; }

    /// <summary>
    /// Base URL of the ShiftIdentity API, if warranty pages need to resolve identity-owned lookups
    /// (companies, users) without depending on ShiftBlazor's ExternalAddresses map.
    /// </summary>
    public string? IdentityApiUrl { get; set; }

    /// <summary>
    /// Route prefix the module's API controllers are mounted under, relative to the HttpClient base
    /// address (e.g. set "WarrantyClaims" when the API uses RoutePrefix "api/WarrantyClaims" and the
    /// HttpClient base already contains "/api/").
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="RoutePrefix"/> normalized with a trailing slash (empty when unset). Pre-pend to any
    /// relative URL (ShiftList EntitySet, ShiftEntityForm Endpoint) that targets the module API.
    /// </summary>
    public string ResolvedRoutePrefix =>
        string.IsNullOrWhiteSpace(RoutePrefix) ? string.Empty : RoutePrefix.Trim('/') + "/";

    /// <summary>
    /// When true, the Blazor UI hides actions/nav items the user lacks warranty TypeAuth permission for.
    /// When false (default), UI behaves as authentication-only.
    /// </summary>
    public bool EnableWarrantyClaimsActionTreeAuthorization { get; set; } = false;
}
