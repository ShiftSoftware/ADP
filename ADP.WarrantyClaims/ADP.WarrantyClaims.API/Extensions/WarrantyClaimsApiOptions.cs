using ShiftSoftware.ADP.WarrantyClaims.Data.Printing;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Extensions;

public class WarrantyClaimsApiOptions
{
    /// <summary>
    /// Optional absolute paths to consumer-supplied .frx templates replacing the module-embedded
    /// print defaults (the sanctioned rebranding hook — e.g. substitute a certificate with your own
    /// legal text). Leave the paths null (default) to print the byte-frozen embedded templates.
    /// </summary>
    public WarrantyClaimsReportOverrides ReportOverrides { get; set; } = new();

    /// <summary>
    /// The TypeAuth action node gating the WarrantyClaim CRUD controller (and the SAS print-invoice
    /// read check). The consumer supplies its OWN node (the original host application passes
    /// its existing <c>WarrantySystem.WarrantyClaim</c> node) so existing permission grants keep
    /// resolving — the module never reproduces the node (decision D9). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? WarrantyClaimAction { get; set; }

    /// <summary>
    /// The TypeAuth boolean node gating the UpdateDeliveryDate override endpoint. In the original host application this
    /// was a <c>[TypeAuth(..., DeliveryDateOverwrite, Access.Maximum)]</c> attribute; the module enforces
    /// the equivalent imperatively against this consumer-supplied node. Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public BooleanAction? DeliveryDateOverwriteAction { get; set; }

    /// <summary>
    /// The TypeAuth read node gating the manufacturer CSV export endpoint (consumer-supplied, per D9 —
    /// the original host application passes <c>WarrantySystem.WarrantyClaimCSV</c>). Checked with READ
    /// access, faithfully preserving the original host's <c>[TypeAuth(..., Access.Read)]</c> attribute:
    /// a mutating export (claims are stamped Exported + the rates row is upserted) gated by a READ
    /// check is a frozen quirk, logged for the standing security pass — do not "fix" it in a move
    /// slice. Only used when <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadAction? CsvExportAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the WarrantyCertificate controller (consumer-supplied, per D9 —
    /// the original host application passes <c>WarrantySystem.Certificates</c>). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? CertificatesAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the WarrantyInvoice controller (consumer-supplied, per D9 —
    /// the original host application passes <c>WarrantySystem.Invoices</c>). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? InvoicesAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the ManufacturerSettlmentSheet controller (consumer-supplied, per
    /// D9 — the original host application passes <c>WarrantySystem.ManufacturerSettlments</c>). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? ManufacturerSettlmentsAction { get; set; }

    /// <summary>
    /// The TypeAuth action node the DistributorFinancial controller passes to its secure base
    /// (consumer-supplied, per D9 — the original host application passes its
    /// <c>WarrantySystem.WarrantyClaim</c> node, exactly what its own controller passed). Only used
    /// when <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? DistributorFinancialAction { get; set; }

    /// <summary>
    /// The TypeAuth action node the DealerFinancial controller passes to its secure base. The
    /// original host application passed <c>null</c> (authentication-only at the controller level;
    /// the Get override still runs the <see cref="WarrantyClaimFinancialAction"/> read check) and
    /// keeps passing null — a lax-auth quirk faithfully preserved by the move and logged for the
    /// standing security pass; do not "fix" it here. Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? DealerFinancialAction { get; set; }

    /// <summary>
    /// The TypeAuth read node both Financial list endpoints check imperatively before serving data
    /// (consumer-supplied, per D9 — the original host application passes
    /// <c>WarrantySystem.WarrantyClaimFinancial</c>). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadAction? WarrantyClaimFinancialAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the AdditionalLaborOperationCode controller
    /// (consumer-supplied, per D9 — the original host application passes
    /// <c>WarrantySystem.AdditionalLaborOperationCodes</c>). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? AdditionalLaborOperationCodesAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the WarrantyRates admin CRUD controller (consumer-supplied,
    /// per D9 — the original host application passes its <c>WarrantySystem.WarrantyClaim</c> node,
    /// exactly what its dissolved Setting controller passed; Phase 3 Slice 3.6, D24). Only used when
    /// <see cref="EnableWarrantyClaimsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? WarrantyRatesAction { get; set; }

    /// <summary>
    /// Route prefix the Warranty API controllers are mounted under (e.g. "api" → "api/WarrantyClaim").
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// When true, endpoints are protected by the consumer-supplied per-action permissions above.
    /// When false (default), endpoints require only authentication.
    /// </summary>
    public bool EnableWarrantyClaimsActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// SQL schema every module-owned entity (and its temporal history table) is placed under. The original
    /// distributor consumer passes <c>null</c> so the existing year-old <c>dbo</c> temporal tables are
    /// NOT renamed under SYSTEM_VERSIONING.
    /// </summary>
    public string? Schema { get; set; } = null;
}
