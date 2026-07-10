using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;

/// <summary>
/// Certificate list DTO — line-type-free, so it serves every claim-type certificate flow (warranty
/// and item-claim lists both render it). Moved VERBATIM from the original host application's Services.Shared.DTOs.Certificate
/// (Phase 2 Slice 6); property names are frozen (OData list wire + Blazor columns).
/// </summary>
public class CertificateListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public long CertificateNo { get; set; }

    public DateTime? CertificateDate { get; set; }

    public DateTime? PeriodStartDate { get; set; }

    public DateTime? PeriodEndDate { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public string? InvoiceID { get { return this.InvoiceDate is not null ? ID : null; } }


    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    public string? CampaignID { get; set; }

    public string? DisplayDistributorCertificateNo { get; set; }
}
