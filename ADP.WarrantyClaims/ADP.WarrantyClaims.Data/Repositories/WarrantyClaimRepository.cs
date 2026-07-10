using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Enums;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The warranty claim repository. Moved from the original host application (Services.Data.Repositories.WarrantyClaimRepository)
/// (Phase 2b) — line replacement, service validation, delivery-date race check + propagation, status
/// transitions and manufacturer CSV export are verbatim.
/// </summary>
/// <remarks>
/// CONSUMER SEAMS:
/// <list type="bullet">
/// <item><see cref="GetDealerShortCodeAsync"/> — the dealer ShortCode used for claim-number auto-numbering on
/// insert. Default (Phase 3 Slice 3.6): the consumer's <see cref="ICompanyInfoProvider"/> registration
/// (branch ShortCode); empty when none is registered. Virtual for per-repository overrides.</item>
/// <item><see cref="ExportManufacturerCSV(string, WarrantyRatesDTO, List{WarrantyClaim})"/> — virtual;
/// the base does the claim mutation + CSV and is the single export entry point. The consumer's
/// warranty-rates persistence (audit-upsert + response echo) rides the
/// <see cref="IWarrantyRatesStore"/> seam, which the module controller invokes BEFORE this method
/// (Phase 3 Slice 3.3) — no derived-repository wrapping.</item>
/// </list>
/// Printing (<see cref="PrintAsync"/> / <see cref="PrintManuufacturerInvoiceAsync"/>) is module-owned
/// (Phase 3 Slice 3.2): embedded .frx (overridable via <c>WarrantyClaimsApiOptions.ReportOverrides</c>)
/// rendered with the consumer's <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/> (required) and
/// <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/> (module default).
/// </remarks>
public class WarrantyClaimRepository :
    ShiftRepository<ShiftDbContext, WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>,
    IShiftEntityPrepareForReplicationAsync<WarrantyClaim>
{
    protected bool isDistributor = false;

    //public bool shouldIncludeCSVEntries = false;
    public bool shouldIncludeLaborLines = false;
    public bool shouldIncludeSubletLines = false;
    public bool shouldIncludePartLines = false;

    private readonly WarrantyClaimService warrantyClaimService;
    private readonly SharedClaimService sharedClaimService;
    private readonly IWarrantyClaimsCapabilityProvider capabilityProvider;
    private readonly DeliveryDateService deliveryDateService;

    public WarrantyClaimRepository(
        ShiftDbContext db,
        WarrantyClaimService warrantyClaimService,
        IWarrantyClaimsCapabilityProvider capabilityProvider,
        SharedClaimService sharedClaimService,
        DeliveryDateService deliveryDateService
    ) : base(db, r =>
    {
        r.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(y => y.WarrantyClaimLaborLines),
            x => x.Include(y => y.WarrantyClaimSubletLines),
            x => x.Include(y => y.WarrantyClaimPartLines)
            //,x => x.Include(y => y.WarrantyClaimCSVEntries) //Required for deleting existing CSV entries before adding new ones
        );
    })
    {
        this.warrantyClaimService = warrantyClaimService;
        this.capabilityProvider = capabilityProvider;
        this.sharedClaimService = sharedClaimService;

        this.isDistributor = this.capabilityProvider.IsDistributor;
        this.deliveryDateService = deliveryDateService;
    }

    /// <summary>
    /// Resolves the dealer ShortCode used to stamp DealerCode / drive claim-number auto-numbering on
    /// insert. Consumer seam (Phase 3 Slice 3.6): the default resolves the consumer's
    /// <see cref="ICompanyInfoProvider"/> lazily (the same registration the module printouts use) and
    /// returns the branch's <see cref="Cases.Shared.Printing.CompanyPrintInfo.ShortCode"/>, so a host
    /// that registers the seam needs no derived repository. When no provider is registered the
    /// pre-3.6 module default (empty string) applies. Kept virtual for consumers that want a
    /// per-repository override.
    /// </summary>
    protected virtual async Task<string> GetDealerShortCodeAsync(long companyBranchID)
    {
        if (PrintingServices.GetService<ICompanyInfoProvider>(this.db) is not { } companyInfoProvider)
            return string.Empty;

        return (await companyInfoProvider.GetBranchAsync("en", companyBranchID)).ShortCode;
    }

    public async ValueTask<WarrantyClaim> PrepareForReplicationAsync(WarrantyClaim entity, ReplicationChangeType changeType)
    {
        var q = await this.GetIQueryable(disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        var newEntity = await q
            .Include(x => x.WarrantyClaimLaborLines)
            .FirstOrDefaultAsync(x => x.ID == entity.ID);

        return newEntity!;
    }

    public override async ValueTask<IQueryable<WarrantyClaim>> GetIQueryable(DateTimeOffset? asOf = null, List<string>? includes = null, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
    {
        var q = await base.GetIQueryable(asOf: asOf, includes: includes, disableDefaultDataLevelAccess: disableDefaultDataLevelAccess, disableGlobalFilters: disableGlobalFilters);

        //if (shouldIncludeCSVEntries)
        //    q = q.Include(x => x.WarrantyClaimCSVEntries);

        if (shouldIncludeLaborLines)
            q = q.Include(x => x.WarrantyClaimLaborLines);

        if (shouldIncludeSubletLines)
            q = q.Include(x => x.WarrantyClaimSubletLines);

        if (shouldIncludePartLines)
            q = q.Include(x => x.WarrantyClaimPartLines);

        return q;
    }

    public override async ValueTask<WarrantyClaimDTO> ViewAsync(WarrantyClaim entity)
    {
        var dto = await base.ViewAsync(entity);

        if (!isDistributor)
        {
            dto.WarrantyClaimLaborLines.ForEach(x => x.DistributorHour = null);
            dto.WarrantyClaimSubletLines.ForEach(x => x.DistributorAmount = null);
            dto.WarrantyClaimPartLines.ForEach(x => x.DistributorPrice = null);
        }

        return dto;
    }

    public override async ValueTask<WarrantyClaim> UpsertAsync(WarrantyClaim entity, WarrantyClaimDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (actionType == ActionTypes.Update)
        {
            this.db.Set<WarrantyClaimLaborLine>().RemoveRange(entity.WarrantyClaimLaborLines.ToList());
            this.db.Set<WarrantyClaimSubletLine>().RemoveRange(entity.WarrantyClaimSubletLines.ToList());
            this.db.Set<WarrantyClaimPartLine>().RemoveRange(entity.WarrantyClaimPartLines.ToList());
            //this.db.WarrantyClaimCSVEntry.RemoveRange(entity.WarrantyClaimCSVEntries.ToList());
        }

        this.warrantyClaimService.WarrantyLinesValidationAndTransformation(actionType, entity, dto, isDistributor);

        //Make sure all validations and transformations are done in the service and not here
        //The service is being tested.

        await this.warrantyClaimService.ValidationAndAssignCalculatedFieldsAsync(actionType, entity, dto, isDistributor);

        var oldDeliveryDate = actionType == ActionTypes.Insert ? null : entity.DeliveryDate;
        var newDeliveryDate = dto.DeliveryDate;
        var deliveryDateChanged = oldDeliveryDate != newDeliveryDate;

        if (deliveryDateChanged && newDeliveryDate.HasValue)
        {
            var verifiedDate = await this.deliveryDateService.GetVerifiedDeliveryDateAsync(
                dto.VIN,
                excludeClaimId: actionType == ActionTypes.Insert ? null : entity.ID
            );

            if (verifiedDate.HasValue && verifiedDate.Value != newDeliveryDate.Value)
            {
                throw new ShiftEntityException(new Message(
                    "Operation Prevented",
                    "A verified Delivery Date already exists for this VIN. Please reload the claim and try again."),
                    additionalData: new Dictionary<string, object> { ["ErrorCode"] = "VerifiedDeliveryDateConflict" }
                );
            }
        }

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        upserted.HasAttachment = dto?.Attachments?.Count > 0;

        if (actionType == ActionTypes.Insert)
        {
            var q = await this.GetIQueryable(disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            var dealerShortCode = await this.GetDealerShortCodeAsync(upserted.CompanyBranchID!.Value);

            await this.warrantyClaimService.AutoGenerateFieldsAsync(q, upserted, dealerShortCode);
        }

        if (deliveryDateChanged && newDeliveryDate.HasValue)
        {
            var affected = await this.deliveryDateService.PropagateDeliveryDateAsync(
                upserted.VIN,
                currentClaimId: upserted.ID,
                newDeliveryDate.Value
            );

            if (affected > 0)
                await this.SaveChangesAsync();
        }

        this.GenerateCSVEntries(entity);

        return upserted;
    }

    public void GenerateCSVEntries(WarrantyClaim entity)
    {
        this.warrantyClaimService.GenerateCSV(entity);
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

        var longId = hashIdService.Decode<WarrantyClaimDTO>(id);

        var claim = (await FindAsync(longId, asOf: null, disableGlobalFilters: true, disableDefaultDataLevelAccess: true))!;

        var distributorInfo = await companyInfoProvider.GetDistributorAsync("en");

        return await new FastReportBuilder()
            .AddFastReportFile(WarrantyClaimsReports.WarrantyClaim(PrintingServices.GetService<WarrantyClaimsReportOverrides>(this.db)))
            .AddDataObject("C", new
            {
                claim.InvoiceNo,
                // Pinned: the byte-frozen WarrantyClaim.frx binds [C.TWCNo]; the entity property
                // renamed to ClaimNumber (Phase 3) but the frx-facing member name must not change.
                TWCNo = claim.ClaimNumber,
                claim.DealerCode,
                claim.DealerClaimNo,
                DateOfReceipt = printOutDateFormatter.GetFormattedDate(claim.DateOfReceipt),
                claim.ProcessFlg,
                claim.WarrantyType,
                claim.Franchise,
                AcInstallDate = printOutDateFormatter.GetFormattedDate(claim.AcInstallDate),
                AcInstallKm = string.Format("{0:#,0}", claim.AcInstallKm),
                claim.ACPreviousRepairOrderNo,
                claim.AP1,
                claim.AP2,
                claim.AP3,
                claim.AP4,
                claim.AP5,
                NV = claim.NV ? "YES" : "",
                claim.VIN_WMI,
                claim.VIN_VDS,
                claim.VIN_CD,
                claim.VIN_VIS,
                DeliveryDate = claim.PreDelivery ? "" : printOutDateFormatter.GetFormattedDate(claim.DeliveryDate),
                RepairDate = printOutDateFormatter.GetFormattedDate(claim.RepairDate),
                Odometer = string.Format("{0:#,0}", claim.Odometer),
                claim.KMFlg,
                claim.RepairOrderNo,
                claim.DataID,
            })
            .AddDataList("Labor", "DataLabor", claim.WarrantyClaimLaborLines.Select(x => new
            {
                x.PayCode,
                MainOperation = x.MainOperation ? "YES" : "",
                x.OperationNumber,
                x.Hour,
                Amount = claim.LaborRate * x.Hour,
            }).ToList<object>())
            .AddDataObject("C2", new
            {
                claim.LaborRate,
                claim.HourTotal,
                claim.LaborTotalAmount,
            })
            .AddDataList("Sublet", "DataSublet", claim.WarrantyClaimSubletLines.Select(x => new
            {
                x.PayCode,
                x.InvoiceNo,
                x.Description,
                x.SubletType,
                x.Amount
            }).ToList<object>())
            .AddDataObject("C3", new
            {
                claim.SubletTotalAmount,
                claim.SubletDescription,
                claim.T1,
                claim.T2,
                claim.T3_1,
                claim.T3_2,
                claim.T3_3,
                claim.T3_4,
                claim.T3_5,
                claim.T3_6,
                claim.T3_7,
                claim.Condition,
                claim.Cause,
                claim.Remedy,
            })
            .AddDataList("Part", "DataPart", claim.WarrantyClaimPartLines.Select(x => new
            {
                x.PayCode,
                OFP = x.OFP ? "YES" : "",
                x.LocalF,
                x.PartNumber,
                x.PartDescription,
                x.Qty,
                x.Price,
            }).ToList<object>())
            .AddDataObject("C4", new
            {
                claim.PartsTotalAmount,
                claim.TotalClaimAmount,
                ProcessDate = printOutDateFormatter.GetFormattedDate(claim.ProcessDate),
                claim.LaborAdjustment,
                claim.SubletAdjustment,
                claim.PartsAdjustment,
                claim.DealerComments,
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text =
                (report.FindObject("TextTitle2") as FastReport.TextObject)!.Text =
                distributorInfo.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation =
                (report.FindObject("PictureLogo2") as FastReport.PictureObject)!.ImageLocation =
                distributorInfo.Logo;
            });
    }

    public async Task UpdateClaimStatusAsync(List<WarrantyClaim> items, UpdateStatusActionTypes actionType, string? inputText)
    {
        this.sharedClaimService.UpdateClaimStatus(
            items.Select(x => (IClaim)x).ToList(),
            actionType, inputText
        );

        //Simple Manufacturer Payment. Without Settlement CSV Upload
        if (actionType == UpdateStatusActionTypes.ManufacturerPaid)
        {
            foreach (var claim in items)
            {
                claim.ManufacturerSettledLaborTotalAmountJPY = claim.LaborTotalAmountDistributorJPY;
                claim.ManufacturerSettledSubletTotalAmountJPY = claim.SubletTotalAmountDistributorJPY;
                claim.ManufacturerSettledPartsTotalAmountJPY = claim.PartsTotalAmountDistributorJPY;

                warrantyClaimService.UpdateManufacturerAmounts(claim);
            }
        }

        await this.SaveChangesAsync();
    }

    /// <summary>
    /// Distributor-only correction tool: reads the VIN of the selected claim(s) and applies <paramref name="newDate"/>
    /// to every claim for that VIN (including verified ones), so a wrongly-locked Delivery Date can be fixed.
    /// Requires the selection to resolve to a single VIN. Returns the number of claims actually changed.
    /// </summary>
    public async Task<int> OverrideDeliveryDateAsync(List<WarrantyClaim> selectedClaims, DateTime newDate)
    {
        if (!this.isDistributor)
            throw new ShiftEntityException(
                new Message("Operation Prevented", "You are not authorized to update Delivery Dates."),
                httpStatusCode: (int)System.Net.HttpStatusCode.Forbidden);

        var vins = selectedClaims
            .Select(x => x.VIN)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (vins.Count == 0)
            throw new ShiftEntityException(new Message("Operation Prevented", "No VIN found in the selected claim(s)."));

        if (vins.Count > 1)
            throw new ShiftEntityException(new Message(
                "Operation Prevented",
                $"The selected claims span {vins.Count} different VINs. Select claims for a single VIN."));

        var affected = await this.deliveryDateService.OverrideDeliveryDateForVinAsync(vins[0], newDate);

        if (affected > 0)
            await this.SaveChangesAsync();

        return affected;
    }

    /// <summary>
    /// Mutates the claims (rates -> amounts, InvoiceNo, ManufacturerStatus=Exported) and returns the
    /// manufacturer CSV stream. Virtual and Setting-free: the consumer's derived repository wraps this to also
    /// upsert its warranty-rates Setting entity before delegating here.
    /// </summary>
    public virtual async Task<Stream> ExportManufacturerCSV(string invoice, WarrantyRatesDTO warrantyRates, List<WarrantyClaim> claims)
    {
        var subErrors = new List<Message>();

        foreach (var item in claims)
        {
            if (item.ManufacturerStatus == WarrantyManufacturerClaimStatus.Paid)
            {
                subErrors.Add(new Message($"Claim #{item.ClaimNumber}", $"has [{WarrantyManufacturerClaimStatus.Paid.Describe()}] status and can not be exported."));
            }

            this.warrantyClaimService.UpdateAmounts(warrantyRates!, item);

            item.InvoiceNo = invoice;

            item.ManufacturerStatus = WarrantyManufacturerClaimStatus.Exported;
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

        return await this.warrantyClaimService.ExportCSVAsync(claims);
    }

    public void UpdateAmounts(WarrantyRatesDTO warrantyRates, WarrantyClaim claim)
    {
        this.warrantyClaimService.UpdateAmounts(warrantyRates, claim);
    }

    // Moved verbatim from the original host's derived repository (Phase 3 Slice 3.2). Method-name
    // typo preserved: the module controller already routes print-invoice through it.
    public virtual async Task<Stream> PrintManuufacturerInvoiceAsync(string id)
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var printOutDateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);

        var distributorInfo = await companyInfoProvider.GetDistributorAsync("en");

        var manufacturerInfo = await companyInfoProvider.GetManufacturerAsync("en");

        var no = 1;

        var claims = await this.db.Set<WarrantyClaim>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.InvoiceNo == id)
            .ToListAsync();

        return await new FastReportBuilder()
            .AddFastReportFile(WarrantyClaimsReports.ManufacturerWarrantyInvoice(PrintingServices.GetService<WarrantyClaimsReportOverrides>(this.db)))
            .AddDataObject("Header", new
            {
                InvoiceNumber = id,
                DisplayDistributorCertificateNo = id,
                Manufacturer = manufacturerInfo.Name,
                Distributor = distributorInfo.Name,
                StartDate = printOutDateFormatter.GetFormattedDate(DateTime.Now),
                EndDate = printOutDateFormatter.GetFormattedDate(DateTime.Now),
                InvoiceDate = printOutDateFormatter.GetFormattedDate(DateTime.Now),
            })
            .AddDataList("Table", "DataClaims", claims.OrderBy(x => x.ID).Select(x => new
            {
                No = no++,
                // Pinned: the byte-frozen ManufacturerWarrantyInvoice.frx binds [Table.TWCNo].
                TWCNo = x.ClaimNumber,
                Franchise = x.Franchise.Substring(0, 1),
                RepairDate = printOutDateFormatter.GetFormattedDate(x.RepairDate),
                VIN = x.VIN,
                Labor = (x.LaborTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Sublet = (x.SubletTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Parts = (x.PartsTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Amount = x.TotalClaimAmountDistributorJPY.ToJPYCurrencyFormat(),
            }).ToList<object>())
            .AddDataObject("Footer", new
            {
                Labor = claims.Sum(x => x.LaborTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Sublet = claims.Sum(x => x.SubletTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Parts = claims.Sum(x => x.PartsTotalAmountDistributorJPY).ToJPYCurrencyFormat(),
                Total = claims.Sum(x => x.TotalClaimAmountDistributorJPY).ToJPYCurrencyFormat(),
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text = distributorInfo.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation = distributorInfo.Logo;
                (report.FindObject("TextFooterAddress") as FastReport.TextObject)!.Text = distributorInfo.Address;
                (report.FindObject("TextFooterContactDetails") as FastReport.TextObject)!.Text = $"{distributorInfo.Phone}\r\n{distributorInfo.Website}";
            });
    }
}
