using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
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
    ) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(x => x.ClaimableItem)
        );

        i.UseGeneratedMapper(map => map

            // ── LIST ─────────────────────────────────────────────────────────────
            // THE "[]" LITERAL IS THE WHOLE POINT. An empty JSON array counts as NO attachment, so
            // the comparison is against both null AND the two-character string "[]". Drop it and
            // every row whose Attachments column holds an empty array flips from No to Yes - a
            // wrong value on every such row, with no error anywhere to notice it.
            //
            // Note this deliberately does NOT read the entity's own `HasAttachment` column, even
            // though one exists and UpsertAsync maintains it. The old profile derived the list
            // value from Attachments instead, and switching to the column would be a behaviour
            // change wherever the two ever disagree.
            // FIVE MORE FLATTENINGS THE OLD PROFILE NEVER MENTIONED - same failure shape as
            // ClaimableItemRepository, same evidence. AutoMapper flattened Campaign.Name,
            // ClaimableItem.Name and the two certificate navigations by name convention; the
            // generated projection does not, and no diagnostic covers list members. The
            // pre-migration baseline carries real values for four of them
            // ("ClaimableItemName": "PARITY-CLAIMABLEITEM parity claimable item",
            // "ReimbursementCertificateCertificateDate": "2020-03-01T00:00:00"), so these are
            // restorations, not additions.
            //
            // EVERY ONE OF THESE IS GUARDED, INCLUDING THE TWO NON-NULLABLE NAVIGATIONS, and that
            // is not defensive padding - it is required for correctness on a second caller.
            //
            // This projection runs in TWO places. The list endpoint runs it as SQL, where an
            // unguarded `e.Campaign.Name` is fine. ItemClaimCertificateRepository.ViewAsync runs the
            // SAME projection IN MEMORY over an already-materialized list (see the note there), and
            // that query Includes ClaimableItem but NOT Campaign - so `e.Campaign` is genuinely null
            // there and an unguarded dereference throws NullReferenceException.
            //
            // The guards also reproduce the old behaviour exactly rather than improving on it: the
            // pre-migration baseline shows this nested list carrying
            // "ClaimableItemName": "PARITY-CLAIMABLEITEM parity claimable item" alongside
            // "CampaignName": null, because AutoMapper's in-memory Map could only flatten what was
            // loaded. Adding an Include to "fix" that null would be a behaviour change.
            .ForList(d => d.CampaignName, e => e.Campaign != null ? e.Campaign.Name : null!)
            .ForList(d => d.ClaimableItemName, e => e.ClaimableItem != null ? e.ClaimableItem.Name : null!)
            .ForList(d => d.ReimbursementCertificateCertificateDate, e => e.ReimbursementCertificate != null ? e.ReimbursementCertificate.CertificateDate : null)
            .ForList(d => d.ReimbursementCertificateInvoiceDate, e => e.ReimbursementCertificate != null ? e.ReimbursementCertificate.InvoiceDate : null)
            .ForList(d => d.ContributionCertificateCertificateDate, e => e.ContributionCertificate != null ? e.ContributionCertificate.CertificateDate : null)
            .ForList(d => d.ContributionCertificateInvoiceDate, e => e.ContributionCertificate != null ? e.ContributionCertificate.InvoiceDate : null)

            .ForList(d => d.HasAttachment, e => e.Attachments == null || e.Attachments == "[]" ? YesNoOptions.No : YesNoOptions.Yes)

            // ── VIEW ─────────────────────────────────────────────────────────────
            // A CASE-SENSITIVITY REGRESSION, caught by SHENGEN004 and fixed here.
            //
            // The DTO member is `CampaignVINEntry` (capital VIN); the entity's key is
            // `CampaignVinEntryID` (lowercase 'in'). The removed .DefaultEntityToDtoAfterMap()
            // looked its foreign key up with InvariantCultureIgnoreCase, so the casing mismatch did
            // not matter and the member WAS populated. The generated convention matches
            // case-sensitively, finds no `CampaignVINEntryID`, and reports the member unmapped -
            // which is exactly why SHENGEN004 names it while it names none of the other select
            // members on this DTO.
            //
            // Left alone this would be a silent read regression: the member would simply start
            // coming back null. Text stays null because CampaignVinEntry has no name-ish column to
            // read - matching what this member carried before (none of these entities declares
            // [ShiftEntityKeyAndName], so every select DTO in this group was Value-only).
            .ForView(d => d.CampaignVINEntry, e => e.CampaignVinEntryID.ToSelectDTO())

            // The other member SHENGEN004 names, and an ignore is correct. There is no
            // ReSubmitForDistributorReview on the entity: it is a client-supplied COMMAND flag,
            // read straight off the DTO in UpsertAsync below to push the claim to PendingProcess,
            // and never persisted. Nothing to read on the way out.
            .IgnoreView(d => d.ReSubmitForDistributorReview)

            // SHENGEN007, and an ignore is the correct answer rather than a mapping.
            //
            // These two flatten a navigation this module does not own: ItemClaim has no
            // VehicleInspectionResult navigation at all, so there is nothing here to project from
            // and AutoMapper left them null too - the pre-migration baseline confirms it
            // ("VehicleInspectionResultVehicleInspectionTypeID": null). They are filled by a
            // CONSUMER's derived repository overriding MapToList with its own join projection, which
            // is the seam this class documents. IgnoreList silences the diagnostic without closing
            // that seam: an override replaces the projection wholesale, so a consumer that fills
            // them still works.
            .IgnoreList(d => d.VehicleInspectionResultVehicleInspectionTypeID)
            .IgnoreList(d => d.VehicleInspectionResultVehicleInspectionTypeName));
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
