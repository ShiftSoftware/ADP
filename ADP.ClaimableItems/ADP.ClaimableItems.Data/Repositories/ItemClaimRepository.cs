using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

/// <summary>
/// The item-claim repository. Moved from the original host application's Services.Data.Repositories.ItemClaimRepository
/// (Phase 2 Slice 5) — the upsert immutability guard and status-transition flow are verbatim.
/// </summary>
/// <remarks>
/// CONSUMER SEAM: register a derived repository (<c>services.AddScoped&lt;ItemClaimRepository,
/// YourItemClaimRepository&gt;()</c> after <c>AddClaimableItemsApiServices</c>) to supply the
/// org-specific pieces the module cannot own:
/// <list type="bullet">
/// <item><see cref="CanModifyPostClaim"/> — permission check for editing non-editable claim fields
/// while in Draft. Default (Phase 3 Slice 3.6): the consumer-supplied
/// <c>ClaimableItemsApiOptions.PostClaimModificationAction</c> TypeAuth node; false when unset.
/// Override only for a non-TypeAuth permission model.</item>
/// <item><see cref="ShiftRepository{DB, EntityType, ListDTO, ViewAndUpsertDTO}.PrintAsync"/> —
/// voucher DATA GATHERING (host: inspection customer info + Cosmos city/country address; not
/// generic). The override collects those inputs and calls the module-owned
/// <see cref="PrintClaimVoucher"/> RENDER method (Phase 3 Slice 3.2).</item>
/// <item><c>MapToList</c> — the ItemClaimListDTO members that flatten consumer-owned navigations
/// (VehicleInspectionResultVehicleInspectionType*) need a consumer-side join projection.</item>
/// </list>
/// </remarks>
public class ItemClaimRepository : ShiftRepository<ShiftDbContext, ItemClaim, ItemClaimListDTO, ItemClaimDTO>
{
    private readonly SharedClaimService sharedClaimService;

    public ItemClaimRepository(
        ShiftDbContext db,
        SharedClaimService sharedClaimService
    ) : base(db, x => x.IncludeRelatedEntitiesWithFindAsync(
        x => x.Include(x => x.ClaimableItem)
    ))
    {
        this.sharedClaimService = sharedClaimService;
    }

    /// <summary>
    /// Whether the current user may modify the otherwise-immutable claim fields of a Draft claim.
    /// Consumer seam (Phase 3 Slice 3.6): when the consumer supplies
    /// <c>ClaimableItemsApiOptions.PostClaimModificationAction</c> the default checks that TypeAuth
    /// node (resolved lazily, like the printing seams), so a host on TypeAuth needs no derived
    /// repository. Without the option the pre-3.6 module default (false) applies. Kept virtual for
    /// consumers with a non-TypeAuth permission model.
    /// </summary>
    protected virtual bool CanModifyPostClaim()
    {
        if (PrintingServices.GetService<ClaimableItemsDataOptions>(this.db)?.PostClaimModificationAction is not { } postClaimModificationAction)
            return false;

        var typeAuthService = PrintingServices.GetRequiredService<ShiftSoftware.TypeAuth.Core.ITypeAuthService>(
            this.db,
            "ClaimableItemsApiOptions.PostClaimModificationAction is set but no ShiftSoftware.TypeAuth.Core.ITypeAuthService " +
            "is registered. Register TypeAuth (services.AddTypeAuth(...)) or leave the option null and override " +
            "ItemClaimRepository.CanModifyPostClaim in a derived repository instead.");

        return typeAuthService.CanAccess(postClaimModificationAction);
    }

    public override async ValueTask<ItemClaim> UpsertAsync(ItemClaim entity, ItemClaimDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var oldEntity = entity.Clone();

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        var postClaimModification = this.CanModifyPostClaim();

        if (actionType == ActionTypes.Update)
        {
            if (!(
                entity.VIN == oldEntity.VIN &&
                entity.ClaimableItemID == oldEntity.ClaimableItemID &&
                entity.CampaignID == oldEntity.CampaignID &&
                entity.VehicleInspectionResultID == oldEntity.VehicleInspectionResultID &&
                entity.ClaimableItemContractID == oldEntity.ClaimableItemContractID &&
                entity.CompanyID == oldEntity.CompanyID &&
                entity.CompanyBranchID == oldEntity.CompanyBranchID &&

                ((entity.ClaimStatus == ClaimStatus.Draft && postClaimModification) ? true : entity.ClaimDate == oldEntity.ClaimDate) &&
                ((entity.ClaimStatus == ClaimStatus.Draft && postClaimModification) ? true : entity.Cost == oldEntity.Cost)
            ))
            {
                throw new ShiftEntityException(
                    message: new Message("Error", "Modifying Claims is not permitted. Only [Attachments, Invoice Number, Job Number] can be modified."),
                    httpStatusCode: (int)HttpStatusCode.Forbidden
                );
            }

            if (dto.ReSubmitForDistributorReview)
                upserted.ClaimStatus = ClaimStatus.PendingProcess;

        }

        upserted.HasAttachment = dto?.Attachments?.Count > 0;

        return upserted;
    }

    public async Task UpdateClaimStatusAsync(List<ItemClaim> items, UpdateStatusActionTypes actionType, string? inputText)
    {
        this.sharedClaimService.UpdateClaimStatus(
            items.Select(x => (IClaim)x).ToList(),
            actionType, inputText
        );

        await this.SaveChangesAsync();
    }

    public override Task<Stream> PrintAsync(string id)
        => throw new NotSupportedException(
            "Item-claim voucher printing needs consumer-gathered customer data. Register a derived ItemClaimRepository whose PrintAsync override collects it and calls PrintClaimVoucher.");

    // Localized-text JSON ({"en":"...","ru":"..."}) → one language. Same helper the original host
    // inspection repository exposes; duplicated here so the module stays consumer-free.
    private static string TranslateLocalized(string jsonString, string lang)
    {
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)![lang];
    }

    /// <summary>
    /// Renders the item-claim voucher PDF — the RENDER half of the voucher split (Phase 3 Slice 3.2),
    /// moved verbatim from the original host. Data gathering stays consumer-side: customer identity,
    /// phone and the resolved address string come in as parameters (the host joins its
    /// VehicleInspectionResult table and resolves the city/country address from identity Cosmos),
    /// as do the two localized signature captions and the print language. Branch/distributor info
    /// flows through the consumer's <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/>; the
    /// .frx is the module-embedded default unless overridden via
    /// <c>ClaimableItemsApiOptions.ReportOverrides</c>.
    /// </summary>
    public async Task<Stream> PrintClaimVoucher(
        string vin,
        DateTimeOffset date,
        long companyBranchID,
        string? fullName,
        string? phone,
        string customerAddress,
        ClaimableItem claimableItem,
        string language,
        string? serviceAdvisorSignatureCaption,
        string? customerSignatureCaption
    )
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var printOutDateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);

        var distributorLogo = (await companyInfoProvider.GetDistributorAsync(language)).Logo;

        var branchInfo = await companyInfoProvider.GetBranchAsync(language, companyBranchID);

        return await new FastReportBuilder()
            .AddFastReportFile(ClaimableItemsReports.ItemClaimVoucher(PrintingServices.GetService<ClaimableItemsReportOverrides>(this.db)))
            .AddDataObject("S", new
            {
                VIN = vin,
                Fullname = fullName,
                Phone = phone,
                ClaimDate = printOutDateFormatter.GetFormattedDateTime(date),
                CustomerAddress = customerAddress,
                Branch = branchInfo.Name,
                BranchPhone = branchInfo.Phone,
                BranchAddress = branchInfo.Address,
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextCustomer") as FastReport.TextObject)!.Text =
                (report.FindObject("TextCustomer2") as FastReport.TextObject)!.Text = """
                [S.Fullname]
                [S.CustomerAddress]
                [S.Phone]
                """;

                (report.FindObject("TextClaim") as FastReport.TextObject)!.Text =
                (report.FindObject("TextClaim2") as FastReport.TextObject)!.Text = """
                <b>[S.VIN]</b>
                [S.ClaimDate]
                """;

                (report.FindObject("TextItemName") as FastReport.TextObject)!.Text =
                (report.FindObject("TextItemName2") as FastReport.TextObject)!.Text = TranslateLocalized(claimableItem.PrintoutTitle!, language);


                (report.FindObject("TextItemDescription") as FastReport.TextObject)!.Text =
                (report.FindObject("TextItemDescription2") as FastReport.TextObject)!.Text = TranslateLocalized(claimableItem.PrintoutDescription!, language);

                (report.FindObject("TextCustomer") as FastReport.TextObject)!.TextRenderType = FastReport.TextRenderType.HtmlTags;
                (report.FindObject("TextCustomer2") as FastReport.TextObject)!.TextRenderType = FastReport.TextRenderType.HtmlTags;
                (report.FindObject("TextClaim") as FastReport.TextObject)!.TextRenderType = FastReport.TextRenderType.HtmlTags;
                (report.FindObject("TextClaim2") as FastReport.TextObject)!.TextRenderType = FastReport.TextRenderType.HtmlTags;

                (report.FindObject("TextServiceAdvisorSignature") as FastReport.TextObject)!.Text =
                (report.FindObject("TextServiceAdvisorSignature2") as FastReport.TextObject)!.Text = serviceAdvisorSignatureCaption;

                (report.FindObject("TextCustomerSignature") as FastReport.TextObject)!.Text =
                (report.FindObject("TextCustomerSignature2") as FastReport.TextObject)!.Text = customerSignatureCaption;

                (report.FindObject("PictureDealerLogo") as FastReport.PictureObject)!.ImageLocation = branchInfo.Logo;
                (report.FindObject("PictureDealerLogo2") as FastReport.PictureObject)!.ImageLocation = branchInfo.Logo;
                (report.FindObject("PictureDistributorLogo") as FastReport.PictureObject)!.ImageLocation = distributorLogo;
                (report.FindObject("PictureDistributorLogo2") as FastReport.PictureObject)!.ImageLocation = distributorLogo;

                (report.FindObject("TextBranch") as FastReport.TextObject)!.Text =
                (report.FindObject("TextBranch2") as FastReport.TextObject)!.Text = """
                [S.Branch]
                [S.BranchPhone]
                [S.BranchAddress]
                """;
            });
    }
}
