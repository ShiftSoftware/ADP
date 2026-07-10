using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

/// <summary>
/// Certifying + invoicing of item claims over the shared ADP.Cases <see cref="Certificate"/>
/// (filtered to <see cref="CertificateTypes.ClaimableItemClaim"/>; <see cref="IsInvoiceMode"/>
/// turns the same repository into the invoice surface). Moved from the original host application (Phase 2 Slice 6) — the
/// certify/un-certify/invoice state tail, single-company/single-campaign rules, and the numbering
/// sequences (CertificateNo per company+type+settlement+campaign; DistributorCertificateNo per year,
/// seed 10 for VehicleInspection-trigger campaigns, campaign-defined display prefix) are verbatim.
/// Printing (<see cref="PrintAsync"/>) is module-owned (Phase 3 Slice 3.2): embedded
/// ItemClaimCertificate/ItemClaimInvoice .frx (overridable via
/// <c>ClaimableItemsApiOptions.ReportOverrides</c>) + Campaign printout templating, rendered with the
/// consumer's <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/> (required) and
/// <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/> (module default).
/// </summary>
public class ItemClaimCertificateRepository : ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, ItemClaimCertificateDTO>
{
    public bool IsInvoiceMode = false;

    private readonly CampaignRepository campaignRepository;
    private readonly AutoMapper.IMapper mapper;

    public ItemClaimCertificateRepository(
        ShiftDbContext db,
        CampaignRepository campaignRepository,
        AutoMapper.IMapper mapper
    ) : base(db)
    {
        this.campaignRepository = campaignRepository;
        this.mapper = mapper;
    }

    // Localized-text JSON ({"en":"...","ru":"..."}) → one language. Same helper the original host
    // inspection repository exposes; duplicated here so the module stays consumer-free.
    private static string TranslateLocalized(string jsonString, string lang)
    {
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)![lang];
    }

    /// <summary>
    /// Formats the dates shown in Invoice() validation messages. Consumer seam (Phase 3 Slice 3.6):
    /// the default resolves the consumer's <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/>
    /// lazily (the same registration the module printouts use), so a host that registers the seam
    /// needs no derived repository. When no formatter is registered the pre-3.6 module default
    /// (yyyy-MM-dd) applies. Kept virtual for consumers that want a per-repository override.
    /// </summary>
    protected virtual string FormatDate(DateTime? date) =>
        PrintingServices.GetService<Cases.Shared.Printing.IPrintoutDateFormatter>(this.db) is { } dateFormatter
            ? dateFormatter.GetFormattedDate(date) ?? ""
            : date?.ToString("yyyy-MM-dd") ?? "";

    public override async ValueTask<ItemClaimCertificateDTO> ViewAsync(Certificate entity)
    {
        var dto = await base.ViewAsync(entity);

        // The Cases Certificate carries no claim collections — load the lines by FK.
        var claims = await this.db.Set<Entities.ItemClaim>()
            .Include(x => x.ClaimableItem)
            .Where(x => x.ReimbursementCertificateID == entity.ID)
            .ToListAsync();

        dto.ReimbursementItemClaims = mapper.Map<List<Shared.DTOs.ItemClaim.ItemClaimListDTO>>(claims);

        return dto;
    }

    public override async ValueTask<IQueryable<Certificate>> GetIQueryable(DateTimeOffset? asOf = null, List<string>? includes = null, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
    {
        var queryable = await base.GetIQueryable(asOf, includes, disableDefaultDataLevelAccess, disableGlobalFilters);

        queryable = queryable.Where(x => x.CertificateType == CertificateTypes.ClaimableItemClaim);

        if (this.IsInvoiceMode)
            queryable = queryable.Where(x => x.InvoiceDate.HasValue);

        return queryable;
    }

    public override async ValueTask<Certificate> UpsertAsync(Certificate entity, ItemClaimCertificateDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (this.IsInvoiceMode)
            throw new ShiftEntityException(new Message("Error", "Invoices can not be saved manually. Please delete this invoice and create a new one from the certificate."));

        if (actionType == ActionTypes.Update && entity.InvoiceDate is not null)
            throw new ShiftEntityException(new Message("Error", "This certificate is already invoiced and can not be updated. Please delete the invoice to update this certificate."));

        // Unlink previously-linked claims by FK (Insert: entity.ID is 0 -> no rows, matching the
        // previously-empty collection).
        var previouslyLinkedClaims = await this.db.Set<Entities.ItemClaim>()
            .Where(x => x.ReimbursementCertificateID == entity.ID)
            .ToListAsync();

        foreach (var item in previouslyLinkedClaims)
        {
            item.ReimbursementCertificateID = null;
            item.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Accepted;
        }

        //ContributionItemClaims (ChargeContributingDealer) flow remains scaffolded/disabled — see D18.

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        upserted.CertificateType = CertificateTypes.ClaimableItemClaim;

        List<long> itemClaimIds = dto.ReimbursementItemClaims
            .Select(x => x.ID?.ToLong())
            .Where(x => x is not null)
            .Select(x => x!.Value!)
            .ToList();

        if (itemClaimIds.Count == 0)
            throw new ShiftEntityException(new Message("Error", "At least one claim must be selected."));

        var claims = await this.db.Set<Entities.ItemClaim>()
            .Include(x => x.ReimbursementCertificate)
            .Include(x => x.Campaign)
            .Where(x => itemClaimIds.Contains(x.ID))
            .ToListAsync();

        var subErrors = new List<Message>();

        if (claims.Select(x => x.CompanyID).Distinct().Count() > 1)
            throw new ShiftEntityException(new Message("Error", "All claims must belong to the same company."));

        if (claims.Select(x => x.CampaignID).Distinct().Count() > 1)
        {
            foreach (var campaign in claims.Select(x => x.Campaign.Name).Distinct())
            {
                subErrors.Add(new Message(TranslateLocalized(campaign, "en")));
            }

            throw new ShiftEntityException(
                new Message(
                    "All claims must belong to the same Campaign. You can not certify claims from the below Campaigns under one certificate:",
                    "",
                    subErrors
                ));
        }

        upserted.CampaignID = claims.First()?.CampaignID;

        claims.Where(x => x.ReimbursementCertificateID != null && x.ReimbursementCertificateID != upserted.ID)
            .ToList()
            .ForEach(x => subErrors.Add(new Message($"Claim #{x.ID}", $"is already certified by certificate #{x.ReimbursementCertificate?.CertificateNo}.")));

        foreach (var item in claims)
        {
            if (actionType == ActionTypes.Insert)
            {
                item.ReimbursementCertificate = upserted;
            }
            else
                item.ReimbursementCertificateID = upserted.ID;

            if (item.ClaimStatus != ShiftSoftware.ADP.Models.Enums.ClaimStatus.Accepted)
            {
                //for(var i = 0; i < 100; i++)
                subErrors.Add(new Message($"Claim #{item.GetClaimIdentifier()}", $"does not have [{ShiftSoftware.ADP.Models.Enums.ClaimStatus.Accepted.Describe()}] status and can not be certified."));
            }

            item.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Certified;
        }

        upserted.CompanyID = claims.FirstOrDefault()?.CompanyID;

        if (actionType == ActionTypes.Insert)
        {
            var q = await this.GetIQueryable(disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            var lastCertificateNo = await q
                .Where(x => x.CompanyID == upserted.CompanyID)
                .Where(x => x.CertificateType == upserted.CertificateType)
                .Where(x => x.SettlementType == upserted.SettlementType)
                .Where(x => x.CampaignID == upserted.CampaignID)
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
                .Where(x => x.CampaignID == upserted.CampaignID)
                .OrderByDescending(x => x.DistributorCertificateNo)
                .Select(x => x.DistributorCertificateNo)
                .FirstOrDefaultAsync();

            var campaign = await this.campaignRepository.FindAsync(upserted.CampaignID!.Value, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            if (lastDistCertificateNo is null || lastDistCertificateNo == 0)
            {
                if (campaign!.ActivationTrigger == ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger.VehicleInspection)
                    lastDistCertificateNo = 10; //First voucher cert no should be 11 as requested
                else
                    lastDistCertificateNo = 0;
            }

            upserted.DistributorCertificateNo = lastDistCertificateNo + 1;

            upserted.DisplayDistributorCertificateNo = $"{campaign?.DistributorCertificateNumberPrefix}{upserted.CertificateDate:yy}-{upserted.DistributorCertificateNo:0000}";
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
        var linkedClaims = await this.db.Set<Entities.ItemClaim>()
            .Where(x => x.ReimbursementCertificateID == entity.ID)
            .ToListAsync();

        if (this.IsInvoiceMode)
        {
            entity.InvoiceDate = null;

            foreach (var item in linkedClaims)
            {
                item.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Certified;
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
                item.ReimbursementCertificateID = null;
                item.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Accepted;
            }

            //ContributionItemClaims (ChargeContributingDealer) flow remains scaffolded/disabled — see D18.

            return deleted;
        }
    }

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

        var claims = await this.db.Set<Entities.ItemClaim>()
            .Where(x => x.ReimbursementCertificateID != null)
            .Where(x => certificateIds.Contains(x.ReimbursementCertificateID!.Value))
            .ToListAsync();

        foreach (var item in claims)
        {
            item.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Invoiced;
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

    // Moved verbatim from the original host's derived repository (Phase 3 Slice 3.2): only the frx
    // source (embedded default / consumer override), the company info (ICompanyInfoProvider), the
    // date formatter (IPrintoutDateFormatter) and the hashid decode type change — everything else,
    // including the Campaign printout templating, is byte-identical.
    public override async Task<Stream> PrintAsync(string id)
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var printOutDateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);
        var hashIdService = PrintingServices.GetRequiredService<IHashIdService>(this.db,
            "Printing requires ShiftSoftware.ShiftEntity.Core.IHashIdService (registered by AddShiftEntityWeb).");

        // Historical quirk (D16): the host always minted/decoded these print tokens with its
        // WarrantyClaimDTO hashid type. ClaimableItems must not reference WarrantyClaims, so the
        // decode uses the module's own ItemClaimCertificateDTO — WIRE-IDENTICAL, because no
        // claim/warranty/certificate DTO carries an ID-level hashid attribute, which makes
        // Decode<T> == long.Parse for every one of these types.
        var longId = hashIdService.Decode<ItemClaimCertificateDTO>(id);

        var certificate = (await FindAsync(longId, asOf: null, disableGlobalFilters: true, disableDefaultDataLevelAccess: true))!;

        // The Cases Certificate carries no claims collection / Campaign navigation — load by FK.
        var certificateClaims = await this.db.Set<Entities.ItemClaim>()
            .Include(x => x.ClaimableItem)
            .Where(x => x.ReimbursementCertificateID == certificate.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        var certificateCampaign = certificate.CampaignID is null
            ? null
            : await this.campaignRepository.FindAsync(certificate.CampaignID.Value, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        var dealerInfo = await companyInfoProvider.GetDealerAsync("en", certificate.CompanyID!.Value);

        var distributorInfo = await companyInfoProvider.GetDistributorAsync("en");

        var no = 1;

        var header = new
        {
            CertificateDate = printOutDateFormatter.GetFormattedDate(certificate.CertificateDate),
            CertificateNo = certificate.CertificateNo,
            certificate.DisplayDistributorCertificateNo,
            Dealer = dealerInfo.Name,
            Distributor = distributorInfo.Name,
            StartDate = printOutDateFormatter.GetFormattedDate(certificate.PeriodStartDate),
            EndDate = printOutDateFormatter.GetFormattedDate(certificate.PeriodEndDate),
            InvoiceDate = printOutDateFormatter.GetFormattedDate(certificate.InvoiceDate),
        };

        var reportOverrides = PrintingServices.GetService<ClaimableItemsReportOverrides>(this.db);

        return await new FastReportBuilder()
            .AddFastReportFile(IsInvoiceMode ? ClaimableItemsReports.ItemClaimInvoice(reportOverrides) : ClaimableItemsReports.ItemClaimCertificate(reportOverrides))
            .AddDataObject("Header", header)
            .AddDataList("Table", "DataClaims", certificateClaims.Select(x => new
            {
                No = no++,
                TWCNo = x.ID,
                Item = TranslateLocalized(x.ClaimableItem.Name, "en"),
                ClaimDate = printOutDateFormatter.GetFormattedDate(x.ClaimDate.Date),
                VIN = x.VIN,
                Amount = x.Cost,
                Model = x.ModelDescription,
                Katashiki = x.Katashiki,
            }).ToList<object>())
            .AddDataObject("Footer", new
            {
                Total = certificateClaims.Sum(x => x.Cost),
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text = distributorInfo.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation = distributorInfo.Logo;
                (report.FindObject("TextFooterAddress") as FastReport.TextObject)!.Text = distributorInfo.Address;
                (report.FindObject("TextFooterContactDetails") as FastReport.TextObject)!.Text = $"{distributorInfo.Phone}\r\n{distributorInfo.Website}";


                (report.FindObject("CellHeading") as FastReport.TextObject)!.Text =
                certificateCampaign?.CertificatePrintoutHeader?
                .Replace($"{{{nameof(header.CertificateDate)}}}", header.CertificateDate)
                .Replace($"{{{nameof(header.CertificateNo)}}}", header.CertificateNo.ToString())
                .Replace($"{{{nameof(header.Dealer)}}}", header.Dealer)
                .Replace($"{{{nameof(header.Distributor)}}}", header.Distributor)
                .Replace($"{{{nameof(header.StartDate)}}}", header.StartDate)
                .Replace($"{{{nameof(header.EndDate)}}}", header.EndDate)
                .Replace($"{{{nameof(header.InvoiceDate)}}}", header.InvoiceDate);


                (report.FindObject("CellBody") as FastReport.TextObject)!.Text =
                certificateCampaign?.CertificatePrintoutBody?
                .Replace($"{{{nameof(header.CertificateDate)}}}", header.CertificateDate)
                .Replace($"{{{nameof(header.CertificateNo)}}}", header.CertificateNo.ToString())
                .Replace($"{{{nameof(header.Dealer)}}}", header.Dealer)
                .Replace($"{{{nameof(header.Distributor)}}}", header.Distributor)
                .Replace($"{{{nameof(header.StartDate)}}}", header.StartDate)
                .Replace($"{{{nameof(header.EndDate)}}}", header.EndDate)
                .Replace($"{{{nameof(header.InvoiceDate)}}}", header.InvoiceDate);

                (report.FindObject("ChildSignStamp") as FastReport.ChildBand)!.Visible = certificateCampaign?.CertificatePrintoutSignStampVisibility ?? false;

                if (!(certificateCampaign?.CertificatePrintoutKatashikiAndModelColumnVisibility ?? false))
                {
                    var columnModelHeader = report.FindObject("ColumnModelKatashikiHeader") as FastReport.Table.TableColumn;
                    var columnModel = report.FindObject("ColumnModelKatashiki") as FastReport.Table.TableColumn;

                    columnModelHeader!.Visible = false;
                    columnModel!.Visible = false;

                    var columnItemHeader = report.FindObject("ColumnItemHeader") as FastReport.Table.TableColumn;
                    var columnItem = report.FindObject("ColumnItem") as FastReport.Table.TableColumn;

                    columnItemHeader!.Width += columnModelHeader.Width;
                    columnItem!.Width += columnModel.Width;
                }
            });
    }
}
