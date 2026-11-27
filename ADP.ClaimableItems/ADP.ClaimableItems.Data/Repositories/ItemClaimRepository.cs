using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.ClaimableItems.Web;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

/// <summary>
/// The item-claim repository: the upsert immutability guard and the status-transition flow.
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
/// <item><see cref="IsDistributor"/> — who may run the distributor's status actions.</item>
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
    ) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(x => x.ClaimableItem)
        );

        // The maps - the list's flattenings and the "[]"-aware HasAttachment, the view's CampaignVINEntry - are in
        // Mappers/ClaimableItemsMapper.cs.
    })
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

        // A save moves nothing: the actions, the certificates and the resubmission below do. A new claim opens as a draft
        // or, from the claim endpoint, as submitted.
        upserted.ClaimStatus =
            actionType == ActionTypes.Update ? oldEntity.ClaimStatus :
            dto.ClaimStatus == ClaimStatus.PendingProcess ? ClaimStatus.PendingProcess : ClaimStatus.Draft;

        upserted.DistributorErrorMessage = oldEntity.DistributorErrorMessage;

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

            if (dto.ReSubmitForDistributorReview && oldEntity.ClaimStatus == ClaimStatus.RejectedWithError)
                upserted.ClaimStatus = ClaimStatus.PendingProcess;

        }

        upserted.HasAttachment = dto?.Attachments?.Count > 0;

        return upserted;
    }

    /// <summary>
    /// Whether the current user may run the distributor's status actions. Consumer seam: the API's
    /// <see cref="IClaimableItemsCapabilityProvider"/> registration decides; without one, Write on
    /// <c>ClaimableItemsApiOptions.CertifyingAction</c>, the rule the claim list shows those actions by. False with
    /// neither.
    /// </summary>
    protected virtual bool IsDistributor() => ClaimableItemsCapability.IsDistributor(this.db);

    public override async ValueTask<ItemClaimDTO> ViewAsync(ItemClaim entity)
    {
        var dto = await base.ViewAsync(entity);

        dto.StatusHistory = await ItemClaimHistory.Instance.ReadAsync(this.db, entity.ID, this.IsDistributor());

        return dto;
    }

    public async Task UpdateClaimStatusAsync(List<ItemClaim> items, UpdateStatusActionTypes actionType, string? inputText)
    {
        ClaimActionAccess.RequireAllowed(actionType, this.IsDistributor());

        this.sharedClaimService.UpdateClaimStatus(
            items.Select(x => (IClaim)x).ToList(),
            actionType, inputText
        );

        ItemClaimHistory.Instance.Record(this.db, inputText);

        await this.SaveChangesAsync();
    }

    /// <summary>One of the form's actions on one claim. <paramref name="comment"/> goes on its history line.</summary>
    public async Task<ItemClaim> ApplyActionAsync(long id, UpdateStatusActionTypes actionType, string? comment)
    {
        ClaimActionAccess.RequireAllowed(actionType, this.IsDistributor());

        var claim = await this.FindForActionAsync(id);

        await this.UpdateClaimStatusAsync([claim], actionType, comment);

        return claim;
    }

    /// <summary>Adds a history line that moves nothing. A save is open to the dealer in every status, so this is too.</summary>
    public async Task<ItemClaim> AddNoteAsync(long id, string? comment)
    {
        var claim = await this.FindForActionAsync(id);

        ItemClaimHistory.Instance.AddNote(this.db, claim, comment);

        await this.SaveChangesAsync();

        return claim;
    }

    private async Task<ItemClaim> FindForActionAsync(long id)
    {
        var claim = await this.FindAsync(id);

        return claim is null || claim.IsDeleted
            ? throw new ShiftEntityException(new Message("Not Found", "This claim does not exist."), httpStatusCode: (int)HttpStatusCode.NotFound)
            : claim;
    }

    public override Task<Stream> PrintAsync(string id)
        => throw new NotSupportedException(
            "Item-claim voucher printing needs consumer-gathered customer data. Register a derived ItemClaimRepository whose PrintAsync override collects it and calls PrintClaimVoucher.");

    // Localized-text JSON ({"en":"...","ru":"..."}) → one language.
    private static string TranslateLocalized(string jsonString, string lang)
    {
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)![lang];
    }

    /// <summary>
    /// Renders the item-claim voucher PDF — the RENDER half of the voucher split (Phase 3 Slice 3.2).
    /// Data gathering stays consumer-side: customer identity,
    /// phone and the resolved address string come in as parameters (a host can join its
    /// VehicleInspectionResult table and resolve the city/country address from its identity data),
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
