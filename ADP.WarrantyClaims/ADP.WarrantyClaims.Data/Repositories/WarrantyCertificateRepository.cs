using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The warranty certificate repository over the shared ADP.Cases <see cref="Certificate"/> entity.
/// Moved from the original host application (Services.Data.Repositories.WarrantyCertificateRepository) (Phase 2b). The Certificate
/// carries no claims collection, so every use is a dependent-side query on WarrantyClaim.CertificateID.
/// The dead <c>IIdentityCosmosDbService</c> dependency the original injected is dropped.
/// </summary>
/// <remarks>
/// CONSUMER SEAM: <see cref="FormatDate"/> — message-text date formatting. Printing
/// (<see cref="PrintAsync"/>) is module-owned (Phase 3 Slice 3.2): embedded WarrantyCertificate/
/// WarrantyInvoice .frx (overridable via <c>WarrantyClaimsApiOptions.ReportOverrides</c>) rendered
/// with the consumer's <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/> (required) and
/// <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/> (module default).
/// </remarks>
public class WarrantyCertificateRepository : ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, CertificateDTO>
{
    protected bool isDistributor = false;
    private readonly IWarrantyClaimsCapabilityProvider capabilityProvider;
    public bool IsInvoiceMode = false;

    private readonly AutoMapper.IMapper mapper;

    // NOTE (Phase 2 Slice 4): Certificate moved to ADP.Cases.Data and no longer carries a
    // WarrantyClaims collection — the old IncludeRelatedEntitiesWithFindAsync is gone; every use
    // of the collection below is a dependent-side query on WarrantyClaim.CertificateID instead.
    public WarrantyCertificateRepository(ShiftDbContext db, IWarrantyClaimsCapabilityProvider capabilityProvider, AutoMapper.IMapper mapper) : base(db)
    {
        this.capabilityProvider = capabilityProvider;

        this.isDistributor = this.capabilityProvider.IsDistributor;

        this.mapper = mapper;
    }

    public override async ValueTask<CertificateDTO> ViewAsync(Certificate entity)
    {
        var dto = await base.ViewAsync(entity);

        // Previously filled by the entity->DTO name-match off the (Include-loaded) collection.
        var claims = await this.db.Set<WarrantyClaim>()
            .Where(x => x.CertificateID == entity.ID)
            .ToListAsync();

        dto.WarrantyClaims = mapper.Map<List<WarrantyCertificateLineDTO>>(claims);

        return dto;
    }

    public override async ValueTask<IQueryable<Certificate>> GetIQueryable(DateTimeOffset? asOf = null, List<string>? includes = null, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
    {
        var queryable = await base.GetIQueryable(asOf: asOf, includes: includes, disableDefaultDataLevelAccess: disableDefaultDataLevelAccess, disableGlobalFilters: disableGlobalFilters);

        queryable = queryable.Where(x => x.CertificateType == CertificateTypes.WarrantyClaim);

        if (this.IsInvoiceMode)
            queryable = queryable.Where(x => x.InvoiceDate.HasValue);

        return queryable;
    }

    public override async ValueTask<Certificate> UpsertAsync(Certificate entity, CertificateDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (this.IsInvoiceMode)
            throw new ShiftEntityException(new Message("Error", "Invoices can not be saved manually. Please delete this invoice and create a new one from the certificate."));

        if (actionType == ActionTypes.Update && entity.InvoiceDate is not null)
            throw new ShiftEntityException(new Message("Error", "This certificate is already invoiced and can not be updated. Please delete the invoice to update this certificate."));

        // Unlink previously-linked claims by FK (Insert: entity.ID is 0 -> no rows, matching the
        // previously-empty collection).
        var previouslyLinkedClaims = await this.db.Set<WarrantyClaim>()
            .Where(x => x.CertificateID == entity.ID)
            .ToListAsync();

        foreach (var item in previouslyLinkedClaims)
        {
            item.CertificateID = null;
            item.ClaimStatus = ClaimStatus.Accepted;
        }

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        upserted.CertificateType = CertificateTypes.WarrantyClaim;

        List<long> warrantyClaimIds = dto.WarrantyClaims
            .Select(x => x.WarrantyClaim?.Value?.ToLong())
            .Where(x => x is not null)
            .Select(x => x!.Value!)
            .ToList();

        if (warrantyClaimIds.Count == 0)
            throw new ShiftEntityException(new Message("Error", "At least one claim must be selected."));

        var claims = await this.db.Set<WarrantyClaim>()
            .Where(x => warrantyClaimIds.Contains(x.ID))
            .ToListAsync();

        var subErrors = new List<Message>();

        if (claims.Select(x => x.CompanyID).Distinct().Count() > 1)
            throw new ShiftEntityException(new Message("Error", "All claims must belong to the same company."));

        foreach (var item in claims)
        {
            if (actionType == ActionTypes.Insert)
                item.Certificate = upserted;
            else
                item.CertificateID = upserted.ID;

            if (item.ClaimStatus != ClaimStatus.Accepted)
            {
                //for(var i = 0; i < 100; i++)
                subErrors.Add(new Message($"Claim #{item.ClaimNumber}", $"does not have [{ClaimStatus.Accepted.Describe()}] status. Only [{ClaimStatus.Accepted.Describe()}] claims can be certified."));
            }

            item.ClaimStatus = ClaimStatus.Certified;
        }

        upserted.CompanyID = claims.FirstOrDefault()?.CompanyID;

        if (actionType == ActionTypes.Insert)
        {
            var q = await this.GetIQueryable(disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            var lastCertificateNo = await q
                .Where(x => x.CompanyID == upserted.CompanyID)
                .Where(x => x.CertificateType == upserted.CertificateType)
                .Where(x => x.SettlementType == upserted.SettlementType)
                .OrderByDescending(x => x.CertificateNo)
                .Select(x => x.CertificateNo)
                .FirstOrDefaultAsync();

            upserted.CertificateNo = lastCertificateNo + 1;



            var year = upserted.CertificateDate!.Value.Year;
            var start = new DateTime(year, 1, 1);
            var end = start.AddYears(1);

            var q2 = await this.GetIQueryable(disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            var lastDistCertificateNo = await q2
                .Where(x => x.CertificateType == upserted.CertificateType)
                .Where(x => x.SettlementType == upserted.SettlementType)
                .Where(x => x.CertificateDate >= start && x.CertificateDate < end)
                .OrderByDescending(x => x.DistributorCertificateNo)
                .Select(x => x.DistributorCertificateNo)
                .FirstOrDefaultAsync();

            if (lastDistCertificateNo is null || lastDistCertificateNo == 0)
                lastDistCertificateNo = 7; //First cert no should be 8 as requested

            upserted.DistributorCertificateNo = lastDistCertificateNo + 1;

            upserted.DisplayDistributorCertificateNo = $"WRAC-{upserted.CertificateDate:yy}-{upserted.DistributorCertificateNo:0000}";
        }

        if (subErrors.Count > 0)
        {
            var errorLimit = 10;

            if (subErrors.Count > errorLimit)
            {
                var remainingErrorCountAfterTakingErrorLimit = subErrors.Count - errorLimit;

                subErrors = subErrors.Take(errorLimit).ToList();

                subErrors.Add(new Message($"+{remainingErrorCountAfterTakingErrorLimit} more claims".ToString(), $"omitted in this warning dialog"));
            }

            throw new ShiftEntityException(
                new Message(
                    "Error",
                    "",
                    subErrors
                ));
        }

        return upserted;
    }

    public override async ValueTask<Certificate> DeleteAsync(Certificate entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var linkedClaims = await this.db.Set<WarrantyClaim>()
            .Where(x => x.CertificateID == entity.ID)
            .ToListAsync();

        if (this.IsInvoiceMode)
        {
            entity.InvoiceDate = null;

            foreach (var item in linkedClaims)
            {
                item.ClaimStatus = ClaimStatus.Certified;
            }

            return entity;
        }
        else
        {
            if (entity.InvoiceDate is not null)
                throw new ShiftEntityException(new Message("Error", "This certificate is already invoiced and can not be deleted. Please delete the invoice to update this certificate."));

            var deleted = await base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);

            foreach (var item in linkedClaims)
            {
                item.CertificateID = null;
                item.ClaimStatus = ClaimStatus.Accepted;
            }

            return deleted;
        }
    }

    // Moved verbatim from the original host's derived repository (Phase 3 Slice 3.2): only the frx
    // source (embedded default / consumer override), the company info (ICompanyInfoProvider) and the
    // date formatter (IPrintoutDateFormatter) go through the consumer seams now.
    public override async Task<Stream> PrintAsync(string id)
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var printOutDateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);
        var hashIdService = PrintingServices.GetRequiredService<IHashIdService>(this.db,
            "Printing requires ShiftSoftware.ShiftEntity.Core.IHashIdService (registered by AddShiftEntityWeb).");

        // Preserved quirk: print tokens for certificates were always minted/decoded with the
        // WarrantyClaimDTO hashid type — decode with the same type (D16).
        var longId = hashIdService.Decode<WarrantyClaimDTO>(id);

        var certificate = (await FindAsync(longId, asOf: null, disableGlobalFilters: true, disableDefaultDataLevelAccess: true))!;

        // Certificate no longer carries the claims collection — load the lines by FK.
        var certificateClaims = await this.db.Set<WarrantyClaim>()
            .Where(x => x.CertificateID == certificate.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        var dealerInfo = await companyInfoProvider.GetDealerAsync("en", certificate.CompanyID!.Value);

        var distributorInfo = await companyInfoProvider.GetDistributorAsync("en");

        var no = 1;

        var reportOverrides = PrintingServices.GetService<WarrantyClaimsReportOverrides>(this.db);

        return await new FastReportBuilder()
            .AddFastReportFile(IsInvoiceMode ? WarrantyClaimsReports.WarrantyInvoice(reportOverrides) : WarrantyClaimsReports.WarrantyCertificate(reportOverrides))
            .AddDataObject("Header", new
            {
                CertificateDate = printOutDateFormatter.GetFormattedDate(certificate.CertificateDate),
                CertificateNo = certificate.CertificateNo,
                certificate.DisplayDistributorCertificateNo,
                Dealer = dealerInfo.Name,
                Distributor = distributorInfo.Name,
                StartDate = printOutDateFormatter.GetFormattedDate(certificate.PeriodStartDate),
                EndDate = printOutDateFormatter.GetFormattedDate(certificate.PeriodEndDate),
                InvoiceDate = printOutDateFormatter.GetFormattedDate(certificate.InvoiceDate),
            })
            .AddDataList("Table", "DataClaims", certificateClaims.Select(x => new
            {
                No = no++,
                // Pinned: the byte-frozen WarrantyCertificate/WarrantyInvoice frx bind [Table.TWCNo];
                // the entity property renamed to ClaimNumber (Phase 3) but the frx-facing member
                // name must not change.
                TWCNo = x.ClaimNumber,
                RepairOrderNo = x.RepairOrderNo,
                RepairDate = printOutDateFormatter.GetFormattedDate(x.RepairDate),
                VIN = x.VIN,
                Labor = x.LaborTotalAmount,
                Sublet = x.SubletTotalAmount,
                Parts = x.PartsTotalAmount,
                Amount = x.TotalClaimAmount,
            }).ToList<object>())
            .AddDataObject("Footer", new
            {
                Labor = certificateClaims.Sum(x => x.LaborTotalAmount),
                Sublet = certificateClaims.Sum(x => x.SubletTotalAmount),
                Parts = certificateClaims.Sum(x => x.PartsTotalAmount),
                Total = certificateClaims.Sum(x => x.TotalClaimAmount),
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text = distributorInfo.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation = distributorInfo.Logo;
                (report.FindObject("TextFooterAddress") as FastReport.TextObject)!.Text = distributorInfo.Address;
                (report.FindObject("TextFooterContactDetails") as FastReport.TextObject)!.Text = $"{distributorInfo.Phone}\r\n{distributorInfo.Website}";
            });
    }

    /// <summary>
    /// Formats a date for user-facing message text (the Invoice validation errors). Consumer seam
    /// (Phase 3 Slice 3.6): the default resolves the consumer's
    /// <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/> lazily (the same registration the
    /// module printouts use), so a host that registers the seam needs no derived repository. When no
    /// formatter is registered the pre-3.6 module default (short date) applies. Kept virtual for
    /// consumers that want a per-repository override.
    /// </summary>
    protected virtual string FormatDate(DateTime? date) =>
        PrintingServices.GetService<Cases.Shared.Printing.IPrintoutDateFormatter>(this.db) is { } dateFormatter
            ? dateFormatter.GetFormattedDate(date) ?? string.Empty
            : date?.ToShortDateString() ?? string.Empty;

    public async Task Invoice(List<Certificate> items, DateTime invoiceDate)
    {
        var subErrors = new List<Message>();

        foreach (var item in items)
        {
            if (item.InvoiceDate is not null)
            {
                subErrors.Add(new Message($"Certificate #{item.CertificateNo}", $"is already invoiced. It can not be invoiced again."));
                continue;
            }

            if (invoiceDate < item.CertificateDate)
            {
                subErrors.Add(new Message($"Certificate #{item.CertificateNo}", $"Is certified on {FormatDate(item.CertificateDate)}. Invoice date ({FormatDate(invoiceDate)}) should be after or on the same day of Certificate Date."));
                continue;
            }

            item.InvoiceDate = invoiceDate;
        }

        var certificateIds = items.Select(x => x.ID).ToList();

        var claims = await this.db.Set<WarrantyClaim>()
            .Where(x => x.CertificateID != null)
            .Where(x => certificateIds.Contains(x.CertificateID!.Value))
            .ToListAsync();

        foreach (var item in claims)
        {
            item.ClaimStatus = ClaimStatus.Invoiced;
        }

        if (subErrors.Count > 0)
        {
            var errorLimit = 10;

            if (subErrors.Count > errorLimit)
            {
                var remainingErrorCountAfterTakingErrorLimit = subErrors.Count - errorLimit;

                subErrors = subErrors.Take(errorLimit).ToList();

                subErrors.Add(new Message($"+{remainingErrorCountAfterTakingErrorLimit} more claims".ToString(), $"omitted in this warning dialog"));
            }

            throw new ShiftEntityException(
                new Message(
                    "Error",
                    "",
                    subErrors
                ));
        }

        await this.SaveChangesAsync();
    }
}
