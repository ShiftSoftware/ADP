
using FileHelpers;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.ManufacturerSettlmentSheet;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using System.Linq;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

public class ManufacturerSettlmentSheetRepository : ShiftRepository<ShiftDbContext, Entities.ManufacturerSettlmentSheet, ManufacturerSettlmentSheetListDTO, ManufacturerSettlmentSheetDTO>
{
    private readonly AzureStorageService azureStorageService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly WarrantyClaimService warrantyClaimService;
    private readonly WarrantyClaimRepository warrantyClaimRepository;

    public ManufacturerSettlmentSheetRepository(
        ShiftDbContext db,
        AzureStorageService azureStorageService,
        IHttpClientFactory httpClientFactory,
        WarrantyClaimService warrantyClaimService,
        WarrantyClaimRepository warrantyClaimRepository
    ) :
        base(db, o => { o.IncludeRelatedEntitiesWithFindAsync(x => x.Include(x => x.WarrantyClaims)); })
    {
        this.azureStorageService = azureStorageService;
        this.httpClientFactory = httpClientFactory;
        this.warrantyClaimService = warrantyClaimService;
        this.warrantyClaimRepository = warrantyClaimRepository;
    }

    public override async ValueTask<ManufacturerSettlmentSheet> UpsertAsync(ManufacturerSettlmentSheet entity, ManufacturerSettlmentSheetDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (actionType == ActionTypes.Update)
        {
            throw new ShiftEntityException(new Message("Error", "Modification is not allowed. Please remove and submit a new one"));
        }

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        var file = dto.Attachments!.First();

        var url = this.azureStorageService.GetSignedURL(file.Blob!, Azure.Storage.Sas.BlobSasPermissions.Read, file.ContainerName, file.AccountName);

        var httpClient = this.httpClientFactory.CreateClient(nameof(ManufacturerSettlmentSheetRepository));

        using var fileResponse = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        fileResponse.EnsureSuccessStatusCode();

        using var stream = await fileResponse.Content.ReadAsStreamAsync();

        var engine = new FileHelperEngine<CSV.WarrantyClaimSettlementCSV>();

        List<CSV.WarrantyClaimSettlementCSV> csvRecords;

        try
        {
            csvRecords = engine.ReadStreamAsList(new StreamReader(stream), -1);
        }
        catch (Exception ex)
        {
            throw new ShiftEntityException(new Message("Invalid CSV", ex.Message));
        }

        var q = await this.warrantyClaimRepository.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: false, disableGlobalFilters: false);

        await this.warrantyClaimService.ManufacturerSettlement(
            csvRecords,
            q,
            upserted
        );

        upserted.InvoiceNumbers = string.Join(
            ", ",
            csvRecords.Select(x => x.InvoiceNo).Distinct()
        );

        return upserted;
    }

    public override async ValueTask<ManufacturerSettlmentSheet> DeleteAsync(ManufacturerSettlmentSheet entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        foreach (var claim in entity.WarrantyClaims)
        {
            claim.ManufacturerSettledLaborTotalAmountJPY = null;
            claim.ManufacturerSettledSubletTotalAmountJPY = null;
            claim.ManufacturerSettledPartsTotalAmountJPY = null;
            claim.ManufacturerSettledTotalClaimAmountJPY = null;
            claim.ManufacturerSettledTotalClaimAmount = null;
            claim.ManufacturerSettlmentSheetID = null;
            claim.ManufacturerStatus = ShiftSoftware.ADP.Models.Enums.WarrantyManufacturerClaimStatus.Exported;
        }

        return await base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public async Task<Stream> GenerateFromClaimsAsync(IQueryable<WarrantyClaim> warrantyClaims, DateTime? startDate, DateTime? endDate, string? invoiceNumber)
    {
        warrantyClaims = warrantyClaims.Where(x => x.ProcessDate != null);

        var allowedClaimStatuses = new List<ClaimStatus?>
        {
            ClaimStatus.Certified,
            ClaimStatus.Invoiced,
        };

        warrantyClaims = warrantyClaims.Where(x => allowedClaimStatuses.Contains(x.ClaimStatus));

        if (startDate is not null)
            warrantyClaims = warrantyClaims.Where(x => x.ProcessDate >= startDate);

        if (endDate is not null)
            warrantyClaims = warrantyClaims.Where(x => x.ProcessDate < endDate);

        var claims = await warrantyClaims.ToListAsync();

        //Assign Invoice Number to Claims
        claims.ForEach(x => x.InvoiceNo = invoiceNumber);

        var engine = new FileHelperEngine<CSV.WarrantyClaimSettlementCSV>();

        var records = claims.Select(x => new CSV.WarrantyClaimSettlementCSV
        {
            InvoiceNo = x.InvoiceNo,
            ClaimNumber = x.ClaimNumber,
            Franchise = x.Franchise.Substring(0, 1),
            WarrantyType = x.WarrantyType,
            ReceiveDate = x.DateOfReceipt?.ToString("dd.MM.yyyy"),
            AmountGrandTotal = x.TotalClaimAmountDistributorJPY ?? 0,
            AmountSettled = x.TotalClaimAmountDistributorJPY ?? 0,

            StatusName = "Processed",

            SettledAmountParts = x.PartsTotalAmountDistributorJPY ?? 0,
            SettledAmountLabor = x.LaborTotalAmountDistributorJPY ?? 0,
            SettledAmountSublet = x.SubletTotalAmountDistributorJPY ?? 0,
            SettledTaxTotal = 0,

            DataId = x.DataID
        }).ToList();

        var textWriter = new StringWriter();

        engine.HeaderText = engine.GetFileHeader();

        engine.WriteStream(textWriter, records);

        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(textWriter.ToString()));
    }
}
