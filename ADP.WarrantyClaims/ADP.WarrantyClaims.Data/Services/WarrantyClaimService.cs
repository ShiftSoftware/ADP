using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ADP.WarrantyClaims.Data.CSV;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using System.Text;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Services;

public class WarrantyClaimService
{
    private readonly IUserClaimService? userClaimService;
    private readonly CosmosClient? cosmosClient;
    private readonly WarrantyClaimsDataOptions? dataOptions;
    private readonly IWarrantyClaimCurrencyResolver currencyResolver;

    public WarrantyClaimService(IUserClaimService? userClaimService = null, CosmosClient? cosmosClient = null, WarrantyClaimsDataOptions? dataOptions = null, IWarrantyClaimCurrencyResolver? currencyResolver = null)
    {
        this.userClaimService = userClaimService;
        this.cosmosClient = cosmosClient;
        this.dataOptions = dataOptions;
        this.currencyResolver = currencyResolver ?? new DefaultWarrantyClaimCurrencyResolver();
    }

    /// <summary>
    /// Sets the claim's currency, rate and labor rate on the DTO before it is validated and mapped, so every
    /// amount the DTO computes uses them. <paramref name="claim"/> is the stored claim as it was loaded.
    /// The currency comes from the host's resolver when the claim is created and never changes afterwards.
    /// The rate is the one sent by someone who may set it, else the stored one, else today's from
    /// <paramref name="currentRate"/>. The labor rates come from the currency's row (<paramref name="currentLaborRate"/>)
    /// when the claim is created and are kept from then on. A currency without a row takes the resolver's, which is
    /// asked again when the rate changes. Once the claim is certified a different rate is rejected.
    /// </summary>
    public async Task ApplyCurrencyAsync(ActionTypes actionType, WarrantyClaim claim, WarrantyClaimDTO dto, long? companyBranchID, bool canSetRate, Func<string, Task<decimal?>> currentRate, Func<string, Task<WarrantyLaborRate?>> currentLaborRate)
    {
        var stored = actionType == ActionTypes.Insert ? null : claim;

        if (stored is not null && IsCurrencyLocked(stored) && canSetRate && dto.CurrencyRate > 0 && dto.CurrencyRate != stored.CurrencyRate)
            throw new ShiftEntityException(new Message(
                "Operation Prevented",
                "The currency rate of a certified claim can not be changed. Remove the Certificate First."),
                additionalData: new Dictionary<string, object> { ["ErrorCode"] = "CurrencyRateLocked" });

        var resolved = await this.ResolveCurrencyAsync(stored, companyBranchID, dto.Franchise, dto.CurrencyRate, canSetRate, currentRate, currentLaborRate);

        if (resolved.CurrencyRate is null || resolved.CurrencyRate <= 0)
            throw new ShiftEntityException(new Message(
                "Missing Currency Rate",
                $"No rate to USD is set for {resolved.Currency}. Add it on the Warranty Rates page."),
                additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingCurrencyRate" });

        if (resolved.LaborRate is not { } laborRate)
            throw new ShiftEntityException(new Message(
                "Missing Labor Rate",
                $"No labor rate is set for {resolved.Currency}. Add it on the Labor Rates page."),
                additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingLaborRate" });

        dto.Currency = resolved.Currency;
        dto.CurrencyRate = resolved.CurrencyRate;
        dto.LaborRate = laborRate;

        // Set by the server only, like the labor rate: whatever the request carries is replaced.
        dto.LaborRateDistributor = resolved.LaborRateDistributor;
    }

    /// <summary>
    /// The currency, rate and labor rate that apply to a claim: <paramref name="stored"/> for a saved claim,
    /// null for a new one. A requested rate counts only from someone who may set it
    /// (<paramref name="canSetRate"/>) and only until the claim is certified. The rate is null when a new
    /// local-currency claim has no rate in the table, and the labor rate when a new claim has none.
    /// </summary>
    public async Task<WarrantyClaimCurrencyDTO> ResolveCurrencyAsync(WarrantyClaim? stored, long? companyBranchID, string? franchise, decimal? requestedRate, bool canSetRate, Func<string, Task<decimal?>> currentRate, Func<string, Task<WarrantyLaborRate?>> currentLaborRate)
    {
        var currency = stored is null
            ? (await this.currencyResolver.GetCurrencyAsync(companyBranchID))?.Trim().ToUpperInvariant()
            : stored.Currency;

        if (DocumentCurrency.IsUSD(currency))
            currency = DocumentCurrency.USD;

        var isUSD = currency == DocumentCurrency.USD;
        var canEditRate = canSetRate && !isUSD && (stored is null || !IsCurrencyLocked(stored));

        decimal? rate;

        if (isUSD)
            rate = 1;
        else if (canEditRate && requestedRate > 0)
            rate = Math.Round(requestedRate.Value, 6); // the column's scale, so the stored USD figures reproduce from the stored rate
        else if (stored is not null)
            rate = stored.CurrencyRate;
        else
            rate = await currentRate(currency!);

        var laborRate = stored?.LaborRate;
        var laborRateDistributor = stored?.LaborRateDistributor;

        if (stored is null || rate != stored.CurrencyRate)
        {
            var row = await currentLaborRate(currency!);

            if (row is not null && stored is null)
            {
                laborRate = row.LaborRate;
                laborRateDistributor = row.LaborRateDistributor;
            }
            else if (row is not null)
            {
                // A labor rate kept in the claim's own currency does not move with the currency's rate to USD, and
                // neither does the distributor's: one that was the dealer's in USD stays at the figure it had.
                laborRateDistributor ??= LaborRateInUSD(stored!);
            }
            else if (rate > 0)
            {
                laborRate = await this.currencyResolver.GetLaborRateAsync(new WarrantyClaimLaborRateContext
                {
                    CompanyBranchID = companyBranchID,
                    Franchise = franchise,
                    Currency = currency!,
                    CurrencyRate = rate.Value,
                }) ?? laborRate;
            }
        }

        return new WarrantyClaimCurrencyDTO
        {
            Currency = currency!,
            CurrencyRate = rate,
            LaborRate = laborRate,
            LaborRateDistributor = laborRateDistributor,
            CanEditRate = canEditRate,
        };
    }

    private static bool IsCurrencyLocked(WarrantyClaim claim) =>
        claim.ClaimStatus is ClaimStatus.Certified or ClaimStatus.Invoiced;

    /// <summary>
    /// Stores the USD equivalents of the dealer-side amounts at the claim's rate. Each total is converted
    /// on its own and the claim total is their sum, so the USD figures add up. A USD claim keeps its
    /// amounts as they are.
    /// </summary>
    public void UpdateCurrencyAmounts(WarrantyClaim claim)
    {
        decimal? toUSD(decimal? amount) => DocumentCurrency.ToUSD(amount, claim.Currency, claim.CurrencyRate);

        claim.LaborRateUSD = toUSD(claim.LaborRate)!.Value;

        foreach (var part in claim.WarrantyClaimPartLines)
            part.PriceUSD = toUSD(part.Price)!.Value;

        foreach (var sublet in claim.WarrantyClaimSubletLines)
            sublet.AmountUSD = toUSD(sublet.Amount)!.Value;

        claim.LaborTotalAmountUSD = toUSD(claim.LaborTotalAmount);
        claim.SubletTotalAmountUSD = toUSD(claim.SubletTotalAmount);
        claim.PartsTotalAmountUSD = toUSD(claim.PartsTotalAmount);

        if (DocumentCurrency.IsUSD(claim.Currency))
            claim.TotalClaimAmountUSD = claim.TotalClaimAmount;
        else
            claim.TotalClaimAmountUSD = claim.LaborTotalAmountUSD + claim.SubletTotalAmountUSD + claim.PartsTotalAmountUSD;
    }

    // The USD labor rate and sublet amount the distributor and manufacturer figures are built on. A USD
    // claim reads the amount itself, so a claim saved before the USD members existed needs no backfill.
    private static decimal LaborRateInUSD(WarrantyClaim claim) =>
        DocumentCurrency.IsUSD(claim.Currency) ? claim.LaborRate : claim.LaborRateUSD;

    // The distributor's labor rate in USD: its own when the host set one, else the dealer's in USD.
    private static decimal DistributorLaborRateInUSD(WarrantyClaim claim) =>
        claim.LaborRateDistributor ?? LaborRateInUSD(claim);

    private static decimal AmountInUSD(WarrantyClaim claim, WarrantyClaimSubletLine sublet) =>
        DocumentCurrency.IsUSD(claim.Currency) ? sublet.Amount : sublet.AmountUSD;

    public void WarrantyLinesValidationAndTransformation(ActionTypes actionType, WarrantyClaim entity, WarrantyClaimDTO dto, bool isDistributor)
    {
        Dictionary<long, WarrantyClaimPartLine> existingPartLines = new();
        Dictionary<long, WarrantyClaimLaborLine> existingLaborLines = new();
        Dictionary<long, WarrantyClaimSubletLine> existingSubletLines = new();

        existingLaborLines = entity.WarrantyClaimLaborLines.ToDictionary(x => x.ID, x => x);
        existingPartLines = entity.WarrantyClaimPartLines.ToDictionary(x => x.ID, x => x);
        existingSubletLines = entity.WarrantyClaimSubletLines.ToDictionary(x => x.ID, x => x);

        if (!isDistributor)
        {
            //Don't accept distributor values from the dealer

            dto.WarrantyClaimLaborLines.ForEach(x => x.DistributorHour = null);
            dto.WarrantyClaimSubletLines.ForEach(x => x.DistributorAmount = null);
            dto.WarrantyClaimPartLines.ForEach(x => x.DistributorPrice = null);
        }

        var requiredLaborFieldCount = 3;

        if (isDistributor)
            requiredLaborFieldCount = 4;

        foreach (var laborDto in dto.WarrantyClaimLaborLines.ToList())
        {
            var providedField = 0;

            if (!string.IsNullOrWhiteSpace(laborDto.PayCode))
                providedField++;

            if (!string.IsNullOrWhiteSpace(laborDto.OperationNumber))
                providedField++;

            if (laborDto.Hour is not null)
                providedField++;

            if (laborDto.DistributorHour is not null)
                providedField++;

            if (providedField != requiredLaborFieldCount)
                throw new ShiftEntityException(new Message("Error in Labor Lines", "Required fields are missing in Labor Lines."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingLaborFields" });

            if (!isDistributor)
            {
                var existingLaborLine = existingLaborLines.GetValueOrDefault(laborDto.ID);

                if (existingLaborLine?.DistributorHour is null)
                {
                    laborDto.DistributorHour = laborDto.Hour;

                    if (dto.WarrantyType == WarrantyTypes.SSC.Key)
                        laborDto.DistributorHour += 0.2m;
                }
                else
                {
                    laborDto.DistributorHour = existingLaborLine.DistributorHour;
                }
            }
        }

        if (dto.WarrantyClaimLaborLines.Count > 0 && dto.WarrantyClaimLaborLines.Count(x => x.MainOperation) != 1)
            throw new ShiftEntityException(new Message("Error in Labor Lines", "Exactly one Labor line should be marked as Main Operation."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MultipleLaborMarkedAsMain" });

        var requiredSubletFieldCount = 5;

        if (isDistributor)
            requiredSubletFieldCount = 6;

        foreach (var subletDto in dto.WarrantyClaimSubletLines.ToList())
        {
            var providedField = 0;

            if (!string.IsNullOrWhiteSpace(subletDto.PayCode))
                providedField++;

            if (!string.IsNullOrWhiteSpace(subletDto.SubletType))
                providedField++;

            if (!string.IsNullOrWhiteSpace(subletDto.InvoiceNo))
                providedField++;

            if (subletDto.Amount is not null)
                providedField++;

            if (!string.IsNullOrWhiteSpace(subletDto.Description))
                providedField++;

            if (subletDto.DistributorAmount is not null)
                providedField++;

            if (providedField != requiredSubletFieldCount)
                throw new ShiftEntityException(new Message("Error in Sublet Lines", "Required fields are missing in Sublet Lines."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingSubletFields" });

            if (!isDistributor)
            {
                var existingSubletLine = existingSubletLines.GetValueOrDefault(subletDto.ID);

                // The distributor amount is in USD; the dealer's amount is in the claim's currency.
                if (existingSubletLine?.DistributorAmount is null)
                    subletDto.DistributorAmount = DocumentCurrency.ToUSD(subletDto.Amount, dto.Currency, dto.CurrencyRate ?? 0);
                else
                    subletDto.DistributorAmount = existingSubletLine.DistributorAmount;
            }
            else if (existingSubletLines.GetValueOrDefault(subletDto.ID) is { } existingSubletLine
                && entity.CurrencyRate != dto.CurrencyRate
                && subletDto.DistributorAmount == existingSubletLine.DistributorAmount
                && existingSubletLine.DistributorAmount == DocumentCurrency.ToUSD(existingSubletLine.Amount, entity.Currency, entity.CurrencyRate))
            {
                // Still the default derived at the old rate, not a distributor's figure: it follows the new rate.
                subletDto.DistributorAmount = DocumentCurrency.ToUSD(subletDto.Amount, dto.Currency, dto.CurrencyRate ?? 0);
            }
        }

        var requiredPartFieldCount = 6;

        if (isDistributor)
            requiredPartFieldCount = 7;

        foreach (var partDto in dto.WarrantyClaimPartLines.ToList())
        {
            var providedField = 0;

            if (!string.IsNullOrWhiteSpace(partDto.PayCode))
                providedField++;

            if (!string.IsNullOrWhiteSpace(partDto.LocalF))
                providedField++;

            if (!string.IsNullOrWhiteSpace(partDto.PartNumber))
                providedField++;

            if (!string.IsNullOrWhiteSpace(partDto.PartDescription))
                providedField++;

            if (partDto.Qty is not null)
                providedField++;

            if (partDto.Price is not null)
                providedField++;

            if (partDto.DistributorPrice is not null)
                providedField++;

            if (providedField != requiredPartFieldCount)
                throw new ShiftEntityException(new Message("Error in Part Lines", "Required fields are missing in Part Lines."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingPartFields" });

            if (!isDistributor)
            {
                var existingPartline = existingPartLines.GetValueOrDefault(partDto.ID);

                if (existingPartline?.DistributorPrice is not null)
                {
                    partDto.DistributorPrice = existingPartline.DistributorPrice;
                }
            }
        }

        if (dto.WarrantyClaimPartLines.Count > 0 && dto.WarrantyClaimPartLines.Count(x => x.OFP) != 1)
            throw new ShiftEntityException(new Message("Error in Part Lines", "Exactly one Part line should be marked as OFP."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MultiplePartMarkedAsOFP" });
    }

    public async Task ValidationAndAssignCalculatedFieldsAsync(ActionTypes actionTypes, WarrantyClaim claim, WarrantyClaimDTO dto, bool isDistributor)
    {
        if (actionTypes == ActionTypes.Update)
        {
            if (isDistributor)
            {
                if (claim.ClaimStatus == ClaimStatus.Draft)
                    throw new ShiftEntityException(new Message(
                        "Unauthorizated",
                        "You don't have permission to modify Claims before being submitted (Dealer Drafts)"),
                        additionalData: new Dictionary<string, object>
                        {
                            ["ErrorCode"] = "DistributorDraftModification"
                        }
                    );
            }
            else
            {
                if (claim.ClaimStatus == ClaimStatus.Draft || claim.ClaimStatus == ClaimStatus.RejectedWithError)
                { }//Can modify
                else
                    throw new ShiftEntityException(new Message(
                        "Operation Prevented",
                        "Only locally saved and returned Claims (Drafts) can be modified."),
                        additionalData: new Dictionary<string, object>
                        {
                            ["ErrorCode"] = "DealerNonDraftModification"
                        }
                    );
            }

            if (dto.ClaimNumber != claim.ClaimNumber)
            {
                throw new ShiftEntityException(new Message(
                    "Operation Prevented",
                    "Claim No cannot be changed."),
                    additionalData: new Dictionary<string, object>
                    {
                        ["ErrorCode"] = "ClaimNumberCannotBeChanged"
                    }
                );
            }

            if (claim.ManufacturerStatus == WarrantyManufacturerClaimStatus.Paid)
                throw new ShiftEntityException(new Message(
                    "Operation Prevented",
                    "Paid Claims can not be modified."),
                    additionalData: new Dictionary<string, object>
                    {
                        ["ErrorCode"] = "PaidClaimModification"
                    }
                );

            if (claim.ClaimStatus == ClaimStatus.Certified)
                throw new ShiftEntityException(new Message(
                    "Operation Prevented",
                    "Certified Claims can not be modified. Remove the Certificate First."),
                    additionalData: new Dictionary<string, object>
                    {
                        ["ErrorCode"] = "CertifiedClaimModification"
                    }
                );

            if (claim.ClaimStatus == ClaimStatus.Invoiced)
                throw new ShiftEntityException(new Message(
                    "Operation Prevented",
                    "Invoiced Claims can not be modified. Remove the Invoice then the Certificate First."),
                    additionalData: new Dictionary<string, object>
                    {
                        ["ErrorCode"] = "InvoicedClaimModification"
                    }
                );
        }
        else
        {
            //if (isDistributor)
            //{
            //    throw new ShiftEntityException(new Message(
            //            "Unauthorizated",
            //            "Only dealers can issue Claims."),
            //            additionalData: new Dictionary<string, object>
            //            {
            //                ["ErrorCode"] = "DistributorClaiming"
            //            }
            //        );
            //}

            if (isDistributor)
                dto.ClaimStatus = ClaimStatus.PendingProcess;
            else
                dto.ClaimStatus = ClaimStatus.Draft;

            dto.ManufacturerStatus = WarrantyManufacturerClaimStatus.NA;
        }

        var claimId = (dto.ID ?? "0").ToLong();

        long? companyId;

        if (actionTypes == ActionTypes.Insert)
            companyId = this.userClaimService?.GetCompanyID();
        else
            companyId = claim.CompanyID;

        //var deliveryDateString = formData["DeliveryDate"].ToString(); //Accepts 00.00.00, Store Minimum SQL Server date if so.
        //claim.DeliveryDate = deliveryDateString.Equals("00.00.00") ? new DateTime(1753, 1, 1) : ShiftSoftware.SettingManager.ParseClientDateTime(deliveryDateString);

        //var acInstallDateString = formData["AcInstallDate"].ToString(); //Accepts 00.00.00, Store Minimum SQL Server date if so.
        //claim.AcInstallDate = acInstallDateString.Equals("00.00.00") ? new DateTime(1753, 1, 1) : ShiftSoftware.SettingManager.ParseClientDateTime(acInstallDateString);

        if (dto.PreDelivery)
            dto.DeliveryDate = null;

        if (dto.WarrantyType.Equals(WarrantyTypes.A1.Key) || dto.WarrantyType.Equals(WarrantyTypes.A2.Key))
        {
            dto.DateOfReceipt = null;
            dto.AP1 = null;
            dto.AP2 = null;
            dto.AP3 = null;
            dto.AP4 = null;
            dto.AP5 = null;

            if (dto.AcInstallDate == null)
                dto.AcInstallDate = dto.DeliveryDate;
        }

        if (dto.WarrantyType.Equals(WarrantyTypes.P2.Key))
        {
            dto.DateOfReceipt = null;
            dto.AP1 = null;
            dto.AP2 = null;
            dto.AP3 = null;
            dto.AP4 = null;
            dto.AP5 = null;

            dto.RepairCompletionDate = dto.RepairDate;
        }

        if (dto.WarrantyType == WarrantyTypes.P2.Key)
            dto.NV = true;

        if (!dto.WarrantyType.Equals(WarrantyTypes.P2.Key)) //For P2, Current Counter Sale Date is stored in this field.
        {
            if (dto.RepairCompletionDate is null)
                dto.RepairCompletionDate = dto.RepairDate;
        }

        dto.LaborOperationNoMain = dto.WarrantyClaimLaborLines.FirstOrDefault(x => x.MainOperation)?.OperationNumber;

        var ofp = dto.WarrantyClaimPartLines.FirstOrDefault(x => x.OFP);

        dto.OFP = ofp?.PartNumber;
        dto.OFPLocalFlag = ofp?.LocalF;

        if (dto.WarrantyType == WarrantyTypes.SSC.Key)
        {
            dto.SSCCampaignCode = await FindSSCCampaignCodeAsync(dto.WarrantyClaimLaborLines);
        }
    }

    // Resolves the claim's SSC campaign code from its labor operation numbers. A campaign matched by the main
    // operation wins; the other operations are only a fallback.
    private async Task<string?> FindSSCCampaignCodeAsync(List<WarrantyClaimLaborLineDTO> laborLines)
    {
        if (cosmosClient is null || laborLines.Count == 0)
            return null;

        var mainOperationNumbers = GetOperationNumbers(laborLines, mainOperation: true);
        var otherOperationNumbers = GetOperationNumbers(laborLines, mainOperation: false);

        return await FindCampaignCodeByLaborCodesAsync(mainOperationNumbers)
            ?? await FindCampaignCodeByLaborCodesAsync(otherOperationNumbers);
    }

    // Operation numbers are hand-entered on the claim, so this is where stray padding and case realistically come
    // from — and normalising here is free. The stored labor codes are matched as-is (see the query below), so this
    // normalises towards the feed's convention rather than meeting it half way.
    private static List<string> GetOperationNumbers(List<WarrantyClaimLaborLineDTO> laborLines, bool mainOperation) =>
        laborLines
            .Where(x => x.MainOperation == mainOperation && !string.IsNullOrWhiteSpace(x.OperationNumber))
            .Select(x => x.OperationNumber!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

    private const string LaborCodeParameterPrefix = "@laborCode";

    // Matching runs server-side so only the matched campaign code crosses the wire, instead of streaming every SSC
    // document to the client and looping over it.
    //
    // Only the Labors array is matched — deliberately not the full SSCAffectedVINModel.EffectiveLabors dual-read,
    // which would also consider the legacy LaborCode1..3 fields. This assumes no SSC document is legacy-only (array
    // absent or empty): such a document yields no campaign code, silently, rather than an error. The assumption
    // holds because every producer now writes the array and the remaining legacy-only records have been re-synced.
    // Restore a fallback branch if a feed is ever replayed from a pre-migration source.
    //
    // The stored labor code is matched bare, with no TRIM()/UPPER() around it. That is deliberate and measured:
    // wrapping it in a function makes the predicate non-sargable, and a claim whose operation numbers match no
    // campaign then loads every SSC document instead of being answered from the index — 376 RU against 3 RU on a
    // ~8k-document container, and it scales linearly with the SSC set. Claim-side normalisation in
    // GetOperationNumbers is free and covers the input that is actually dirty; the feed is machine-generated.
    // Matching is therefore case- and whitespace-sensitive on the stored side. If a feed ever lands padded or
    // lower-cased codes, normalise them at ingestion rather than reintroducing functions here.
    //
    // IN (not ARRAY_CONTAINS over a parameter array) keeps the predicate index-served for the several labor lines
    // a claim can carry — ARRAY_CONTAINS with the parameter array as the haystack is not indexable.
    //
    // Field names come from nameof() rather than string literals: raw SQL over an absent property yields null, not
    // an error, which is exactly how this query silently stopped matching when SSC records moved to the Labors
    // array. Only nameof() constants and generated parameter names are interpolated — every runtime value is
    // passed as a @parameter.
    private static string BuildCampaignCodeByLaborCodeQuery(int laborCodeCount)
    {
        var laborCodes = string.Join(", ", Enumerable.Range(0, laborCodeCount).Select(i => $"{LaborCodeParameterPrefix}{i}"));

        return $@"
        SELECT VALUE c.{nameof(SSCAffectedVINModel.CampaignCode)}
        FROM c
        WHERE c.{nameof(SSCAffectedVINModel.ItemType)} = @itemType
        AND EXISTS(
            SELECT VALUE l
            FROM l IN c.{nameof(SSCAffectedVINModel.Labors)}
            WHERE l.{nameof(SSCLaborLineModel.LaborCode)} IN ({laborCodes})
        )
        OFFSET 0 LIMIT 1";
    }

    private async Task<string?> FindCampaignCodeByLaborCodesAsync(List<string> laborCodes)
    {
        if (laborCodes.Count == 0)
            return null;

        var container = cosmosClient!.GetContainer(
            NoSQLConstants.Databases.CompanyData,
            NoSQLConstants.Containers.Vehicles
        );

        var queryDefinition = new QueryDefinition(BuildCampaignCodeByLaborCodeQuery(laborCodes.Count))
            .WithParameter("@itemType", (string)ModelTypes.SSCAffectedVIN);

        for (var i = 0; i < laborCodes.Count; i++)
            queryDefinition.WithParameter($"{LaborCodeParameterPrefix}{i}", laborCodes[i]);

        var iterator = container.GetItemQueryIterator<string>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            foreach (var campaignCode in await iterator.ReadNextAsync())
                return campaignCode;
        }

        return null;
    }

    public List<WarrantyClaimManufacturerCSV> GenerateCSV(WarrantyClaim claim)
    {
        var csvEntries = new List<WarrantyClaimManufacturerCSV>();

        var laborPageCount = Math.Ceiling((claim.WarrantyClaimLaborLines.Count / 3.0));
        var subletPageCount = Math.Ceiling((claim.WarrantyClaimSubletLines.Count / 2.0));
        var partsPageCount = Math.Ceiling((claim.WarrantyClaimPartLines.Count / 6.0));

        var pageCount = Math.Max(laborPageCount, Math.Max(subletPageCount, partsPageCount));

        if (pageCount == 0)
            pageCount = 1;

        var consumedLaborCount = 0;
        var consumedSubletCount = 0;
        var consumedPartsCount = 0;

        for (int i = 0; i < pageCount; i++)
        {
            var csvEntry = new WarrantyClaimManufacturerCSV();

            //csvEntry.CompanyID = claim.CompanyID;

            //csvEntry.ClaimStatus = claim.ClaimStatus;
            //csvEntry.ManufacturerStatus = claim.ManufacturerStatus;
            csvEntry.Page = i + 1;

            // FDIST and CLMNT both carry the host's distributor code (WarrantyClaimsApiOptions.DistributorCode).
            csvEntry.DistCode = this.dataOptions?.DistributorCode;
            csvEntry.ClaimantCode = this.dataOptions?.DistributorCode;

            csvEntry.ClaimNumber = claim.ClaimNumber;
            csvEntry.InvoiceNo = claim.InvoiceNo;
            csvEntry.DealerCode = claim.DealerCode;
            csvEntry.DealerClaimNo = claim.DealerClaimNo;
            //csvEntry.DateOfReceipt = claim.DateOfReceipt;
            csvEntry.ProcessFlg = (int)claim.ProcessFlg;

            csvEntry.BatteryTestCode11 = claim.BatteryTestCode11;
            csvEntry.BatteryTestCode12 = claim.BatteryTestCode12;
            csvEntry.BatteryTestCode21 = claim.BatteryTestCode21;
            csvEntry.BatteryTestCode22 = claim.BatteryTestCode22;

            csvEntry.TSB = claim.TSB;

            csvEntry.WarrantyType = claim.WarrantyType;
            csvEntry.OperationType = (int)claim.OperationType;
            if (csvEntry.WarrantyType.Equals(WarrantyTypes.SSC.Key))
                csvEntry.WarrantyType = WarrantyTypes.VE.Key;


            csvEntry.Franchise = claim.Franchise;
            csvEntry.AP1 = claim.AP1;
            csvEntry.AP2 = claim.AP2;
            csvEntry.AP3 = claim.AP3;
            csvEntry.AP4 = claim.AP4;
            csvEntry.AP5 = claim.AP5;
            csvEntry.NV = claim.NV;
            //csvEntry.FV = claim.FV;
            csvEntry.VIN_WMI = claim.VIN_WMI;
            csvEntry.VIN_VDS = claim.VIN_VDS;
            csvEntry.VIN_CD = claim.VIN_CD;
            csvEntry.VIN_VIS = claim.VIN_VIS;
            csvEntry.DeliveryDate = claim.PreDelivery
                ? WarrantyClaimManufacturerCSV_DateConverter.PreDeliverySentinel
                : claim.DeliveryDate;
            csvEntry.RepairOpenDate = claim.RepairDate;
            csvEntry.RepairCompletionDate = claim.RepairCompletionDate;
            csvEntry.Odometer = claim.Odometer;
            csvEntry.KMFlg = (int)claim.KMFlg;
            csvEntry.RepairOrderNo = claim.RepairOrderNo;


            csvEntry.InvoiceCurrency = claim.InvoiceCurrency;
            csvEntry.PRR1 = claim.PRR1;

            csvEntry.ExchangeRate = claim.SubletExchangeRate;

            csvEntry.LaborTotalAmount = claim.InvoiceCurrency == 1 ? claim.LaborTotalAmountDistributorJPY : claim.LaborTotalAmountDistributor;
            csvEntry.SubletTotalAmount = claim.InvoiceCurrency == 1 ? claim.SubletTotalAmountDistributorJPY : claim.SubletTotalAmountDistributor;
            csvEntry.PartsTotalAmount = claim.InvoiceCurrency == 1 ? claim.PartsTotalAmountDistributorJPY : claim.PartsTotalAmountDistributor;
            csvEntry.TotalClaimAmount = claim.InvoiceCurrency == 1 ? claim.TotalClaimAmountDistributorJPY : claim.TotalClaimAmountDistributor;

            csvEntry.DataID = claim.DataID;

            #region Labor

            var laborIndex = 3 * i;
            if (laborIndex < claim.WarrantyClaimLaborLines.Count)
            {
                var currentLaborItem = claim.WarrantyClaimLaborLines.ElementAt(laborIndex);

                csvEntry.LaborPayCode1 = currentLaborItem.PayCode;
                csvEntry.LaborOperationNo1 = currentLaborItem.OperationNumber;

                //Enter the Flat Rate Time (Hour) of each operation to the first decimal place, WPP Manual, P76
                csvEntry.LaborHour1 = currentLaborItem.DistributorHour!.Value;

                consumedLaborCount++;
            }
            laborIndex++;
            if (laborIndex < claim.WarrantyClaimLaborLines.Count)
            {
                var currentLaborItem = claim.WarrantyClaimLaborLines.ElementAt(laborIndex);

                csvEntry.LaborPayCode2 = currentLaborItem.PayCode;
                csvEntry.LaborOperationNo2 = currentLaborItem.OperationNumber;

                //Enter the Flat Rate Time (Hour) of each operation to the first decimal place, WPP Manual, P76
                csvEntry.LaborHour2 = currentLaborItem.DistributorHour!.Value;

                consumedLaborCount++;
            }
            laborIndex++;
            if (laborIndex < claim.WarrantyClaimLaborLines.Count)
            {
                var currentLaborItem = claim.WarrantyClaimLaborLines.ElementAt(laborIndex);

                csvEntry.LaborPayCode3 = currentLaborItem.PayCode;
                csvEntry.LaborOperationNo3 = currentLaborItem.OperationNumber;

                //Enter the Flat Rate Time (Hour) of each operation to the first decimal place, WPP Manual, P76
                csvEntry.LaborHour3 = currentLaborItem.DistributorHour!.Value;

                consumedLaborCount++;
            }

            #endregion
            csvEntry.LaborOperationNoMain = claim.LaborOperationNoMain;

            //to second decimal place, as demonstrated on the WPP Manual
            csvEntry.LaborRate = claim.LaborRateJPY;

            //if (consumedLaborCount == claim.WarrantyClaimLaborLines.Count)
            {
                //Enter the total of labor hours to the first decimal place, WPP Manual, P84
                csvEntry.HourTotal = claim.HourTotalDistributor!.Value;

                //Totals are set only on the row containing the last item, once occcured, the counter is set to -1 so that no other rows have this total
                consumedLaborCount = -1;
            }

            #region Sublet

            var subletIndex = 2 * i;
            if (subletIndex < claim.WarrantyClaimSubletLines.Count)
            {
                var currentSubletItem = claim.WarrantyClaimSubletLines.ElementAt(subletIndex);

                csvEntry.SubletPayCode1 = currentSubletItem.PayCode;
                csvEntry.SubletType1 = currentSubletItem.SubletType!;
                csvEntry.SubletInvoiceNo1 = currentSubletItem.InvoiceNo!;

                //to second decimal place, as demonstrated on the WPP Manual. The distributor amount is what is claimed from the manufacturer.
                csvEntry.SubletAmount1 = (claim.InvoiceCurrency == 1 ? currentSubletItem.DistributorAmountJPY : currentSubletItem.DistributorAmount) ?? 0m;
                csvEntry.SubletDescription = currentSubletItem.Description!;

                consumedSubletCount++;
            }

            subletIndex++;
            if (subletIndex < claim.WarrantyClaimSubletLines.Count)
            {
                var currentSubletItem = claim.WarrantyClaimSubletLines.ElementAt(subletIndex);

                csvEntry.SubletPayCode2 = currentSubletItem.PayCode;
                csvEntry.SubletType2 = currentSubletItem.SubletType!;
                csvEntry.SubletInvoiceNo2 = currentSubletItem.InvoiceNo!;

                //to second decimal place, as demonstrated on the WPP Manual. The distributor amount is what is claimed from the manufacturer.
                csvEntry.SubletAmount2 = (claim.InvoiceCurrency == 1 ? currentSubletItem.DistributorAmountJPY : currentSubletItem.DistributorAmount) ?? 0m;
                csvEntry.SubletDescription2 = currentSubletItem.Description!;

                consumedSubletCount++;
            }

            #endregion

            //if (consumedSubletCount == claim.WarrantyClaimSubletLines.Count)
            {
                //to second decimal place, as demonstrated on the WPP Manual. Rounded up, as observed from the sample exported files.
                //Totals are set only on the row containing the last item, once occcured, the counter is set to -1 so that no other rows have this total
                consumedSubletCount = -1;
            }

            csvEntry.T1 = claim.T1;
            csvEntry.T2 = claim.T2;
            csvEntry.T3_1 = claim.T3_1;
            csvEntry.T3_2 = claim.T3_2;
            csvEntry.T3_3 = claim.T3_3;
            csvEntry.T3_4 = claim.T3_4;
            csvEntry.T3_5 = claim.T3_5;
            csvEntry.T3_6 = claim.T3_6;
            csvEntry.T3_7 = claim.T3_7;
            csvEntry.Condition = claim.Condition;
            csvEntry.Cause = claim.Cause;
            csvEntry.Remedy = claim.Remedy;
            #region Parts

            var partsIndex = 6 * i;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode1 = currentPartsItem.PayCode;
                csvEntry.PartsLF1 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo1 = currentPartsItem.PartNumber;
                csvEntry.PartsQty1 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount1 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            partsIndex++;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode2 = currentPartsItem.PayCode;
                csvEntry.PartsLF2 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo2 = currentPartsItem.PartNumber;
                csvEntry.PartsQty2 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount2 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            partsIndex++;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode3 = currentPartsItem.PayCode;
                csvEntry.PartsLF3 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo3 = currentPartsItem.PartNumber;
                csvEntry.PartsQty3 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount3 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            partsIndex++;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode4 = currentPartsItem.PayCode;
                csvEntry.PartsLF4 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo4 = currentPartsItem.PartNumber;
                csvEntry.PartsQty4 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount4 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            partsIndex++;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode5 = currentPartsItem.PayCode;
                csvEntry.PartsLF5 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo5 = currentPartsItem.PartNumber;
                csvEntry.PartsQty5 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount5 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            partsIndex++;
            if (partsIndex < claim.WarrantyClaimPartLines.Count)
            {
                var currentPartsItem = claim.WarrantyClaimPartLines.ElementAt(partsIndex);

                csvEntry.PartsPayCode6 = currentPartsItem.PayCode;
                csvEntry.PartsLF6 = currentPartsItem.LocalF;
                csvEntry.PartsPartsNo6 = currentPartsItem.PartNumber;
                csvEntry.PartsQty6 = currentPartsItem.Qty!.Value;

                csvEntry.PartsAmount6 = claim.PRR1!.Value * (currentPartsItem.Qty ?? 0m) * (claim.InvoiceCurrency == 1 ? currentPartsItem.DistributorPriceJPY : currentPartsItem.DistributorPrice) ?? 0m;

                consumedPartsCount++;
            }

            #endregion
            csvEntry.OFPLocalFlag = claim.OFPLocalFlag;
            csvEntry.OFP = claim.OFP;

            //if (consumedPartsCount == claim.WarrantyClaimPartLines.Count)
            {
                //to second decimal place, as demonstrated on the WPP Manual. Rounded up, as observed from the sample exported files.

                //Totals are set only on the row containing the last item, once occcured, the counter is set to -1 so that no other rows have this total
                consumedPartsCount = -1;
            }

            //if (i + 1 == pageCount)
            {
                //to second decimal place, as demonstrated on the WPP Manual. Rounded up, as observed from the sample exported files.
            }

            csvEntry.ProcessDate = claim.ProcessDate;
            csvEntry.InitialProcessDate = claim.ProcessDate;
            csvEntry.LaborAdjustment = claim.LaborAdjustment;
            csvEntry.SubletAdjustment = claim.SubletAdjustment;
            csvEntry.PartsAdjustment = claim.PartsAdjustment;
            csvEntry.DistComment1 = claim.DistComment1;

            var isACWarranty = claim.WarrantyType.Equals(WarrantyTypes.A1.Key) || claim.WarrantyType.Equals(WarrantyTypes.A2.Key);
            csvEntry.AcInstallDate = claim.PreDelivery && isACWarranty
                ? WarrantyClaimManufacturerCSV_DateConverter.PreDeliverySentinel
                : claim.AcInstallDate;

            if (claim.AcInstallKm.HasValue)
                csvEntry.AcInstallKm = claim.AcInstallKm.Value;

            csvEntry.ACPreviousRepairOrderNo = claim.ACPreviousRepairOrderNo;
            csvEntry.AcPreviousRepairDate = claim.AcPreviousRepairDate;

            if (claim.AcPreviousRepairKm.HasValue)
                csvEntry.AcPreviousRepairKm = claim.AcPreviousRepairKm.Value;

            csvEntry.AcPreviousInvoiceNo = claim.AcPreviousInvoiceNo;
            csvEntry.AcCurrentInvoiceNo = claim.AcCurrentInvoiceNo;


            csvEntries.Add(csvEntry);
        }

        return csvEntries;
    }

    public async Task<MemoryStream> ExportCSVAsync(List<Entities.WarrantyClaim> claims)
    {
        var memoryStream = new MemoryStream();

        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        using (var streamWriter = new StreamWriter(memoryStream, utf8WithoutBom, leaveOpen: true))
        {
            var engine = new FileHelpers.FileHelperEngine<WarrantyClaimManufacturerCSV>(Encoding.UTF8);

            engine.HeaderText = engine.GetFileHeader();

            engine.WriteStream(streamWriter, claims.SelectMany(x => GenerateCSV(x)));

            await streamWriter.FlushAsync();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    public void UpdateAmounts(WarrantyRatesDTO warrantyRates, WarrantyClaim claim)
    {
        claim.LaborExchangeRate = warrantyRates.LaborExchangeRate;
        claim.SubletExchangeRate = warrantyRates.SubletExchangeRate;
        claim.PartExchangeRate = warrantyRates.PartExchangeRate;
        claim.PRR1 = warrantyRates.PRR;

        this.UpdateDistributorAmounts(claim);

        this.UpdateManufacturerAmounts(claim);
    }

    /// <summary>
    /// A save computes the yen without rounding the yen rates up, as the export does. On a claim that was already
    /// exported the figures are computed again the export's way, so a save does not move them off it.
    /// </summary>
    public void KeepExportRounding(WarrantyClaim claim)
    {
        if (claim.ManufacturerStatus is null or WarrantyManufacturerClaimStatus.NA)
            return;

        if (claim.LaborExchangeRate is null || claim.SubletExchangeRate is null || claim.PartExchangeRate is null || claim.PRR1 is null)
            return;

        this.UpdateDistributorAmounts(claim);
    }

    /// <summary>
    /// The distributor's labor in USD and JPY, from the claim's distributor labor rate and hours. The claim's
    /// distributor totals move by the difference. The yen need the claim's labor exchange rate and stay as they are
    /// without it.
    /// </summary>
    public void UpdateDistributorLabor(WarrantyClaim claim)
    {
        // The yen rate is per USD, so the rate is taken in USD. It is the distributor's: what the manufacturer is
        // charged, which can differ from what the dealer is paid.
        var laborRateUSD = DistributorLaborRateInUSD(claim);

        var labor = claim.HourTotalDistributor * laborRateUSD;
        claim.TotalClaimAmountDistributor += (labor ?? 0) - (claim.LaborTotalAmountDistributor ?? 0);
        claim.LaborTotalAmountDistributor = labor;

        if (claim.LaborExchangeRate is not { } yenPerUSD)
            return;

        claim.LaborRateJPY = Math.Ceiling(laborRateUSD * yenPerUSD);

        var laborJPY = claim.HourTotalDistributor * claim.LaborRateJPY;
        claim.TotalClaimAmountDistributorJPY += (laborJPY ?? 0) - (claim.LaborTotalAmountDistributorJPY ?? 0);
        claim.LaborTotalAmountDistributorJPY = laborJPY;
    }

    private void UpdateDistributorAmounts(WarrantyClaim claim)
    {
        this.UpdateDistributorLabor(claim);

        // The yen rates are per USD, so the inputs are taken in USD.
        claim.WarrantyClaimSubletLines.ToList().ForEach(x => x.AmountJPY = Math.Ceiling(AmountInUSD(claim, x) * claim.SubletExchangeRate.Value));
        claim.WarrantyClaimSubletLines.ToList().ForEach(x => x.DistributorAmountJPY = Math.Ceiling((x.DistributorAmount ?? 0) * claim.SubletExchangeRate.Value));
        claim.WarrantyClaimPartLines.ToList().ForEach(x => x.DistributorPriceJPY = Math.Ceiling((x.DistributorPrice ?? 0) * claim.PartExchangeRate.Value));


        claim.SubletTotalAmount = claim.WarrantyClaimSubletLines.Sum(x => x.Amount);
        claim.SubletTotalAmountDistributor = claim.WarrantyClaimSubletLines.Sum(x => x.DistributorAmount ?? 0m);
        claim.PartsTotalAmountDistributor = claim.WarrantyClaimPartLines.Sum(x => (x.DistributorPrice ?? 0m) * (x.Qty ?? 0m)) * claim.PRR1;


        claim.SubletTotalAmountJPY = claim.WarrantyClaimSubletLines.Sum(x => x.AmountJPY);
        claim.SubletTotalAmountDistributorJPY = claim.WarrantyClaimSubletLines.Sum(x => x.DistributorAmountJPY ?? 0m);
        claim.PartsTotalAmountDistributorJPY = claim.WarrantyClaimPartLines.Sum(x => (x.DistributorPriceJPY ?? 0m) * (x.Qty ?? 0m)) * claim.PRR1.Value;

        claim.TotalClaimAmountDistributor = claim.LaborTotalAmountDistributor + claim.SubletTotalAmountDistributor + claim.PartsTotalAmountDistributor;
        claim.TotalClaimAmountDistributorJPY = claim.LaborTotalAmountDistributorJPY + claim.SubletTotalAmountDistributorJPY + claim.PartsTotalAmountDistributorJPY;
    }

    public void UpdateManufacturerAmounts(WarrantyClaim claim)
    {
        var totalJPY =
            (claim.ManufacturerSettledLaborTotalAmountJPY ?? 0m) +
            (claim.ManufacturerSettledSubletTotalAmountJPY ?? 0m) +
            (claim.ManufacturerSettledPartsTotalAmountJPY ?? 0m);

        if (totalJPY != 0m)
            claim.ManufacturerSettledTotalClaimAmountJPY = totalJPY;
        else
            claim.ManufacturerSettledTotalClaimAmountJPY = null;

        var manufacturerSettledLaborTotalAmount = (claim.ManufacturerSettledLaborTotalAmountJPY ?? 0m) / claim.ManufacturerSettlmentSheet?.ExchangeRate;
        var manufacturerSettledSubletTotalAmount = (claim.ManufacturerSettledSubletTotalAmountJPY ?? 0m) / claim.ManufacturerSettlmentSheet?.ExchangeRate;
        var manufacturerSettledPartsTotalAmount = (claim.ManufacturerSettledPartsTotalAmountJPY ?? 0m) / claim.ManufacturerSettlmentSheet?.ExchangeRate;

        var total =
            manufacturerSettledLaborTotalAmount +
            manufacturerSettledSubletTotalAmount +
            manufacturerSettledPartsTotalAmount;

        if (total != 0)
            claim.ManufacturerSettledTotalClaimAmount = total;
        else
            claim.ManufacturerSettledTotalClaimAmount = null;
    }

    public async Task ManufacturerSettlement(List<CSV.WarrantyClaimSettlementCSV> settlementClaims, IQueryable<Entities.WarrantyClaim> warrantyClaims, Entities.ManufacturerSettlmentSheet manufacturerSettlmentSheet)
    {
        settlementClaims = settlementClaims
            .Where(x => x.StatusName == "Processed")
            .ToList();

        var claimNumbers = settlementClaims
            .Select(x => $"{x.ClaimNumber}-{x.InvoiceNo}")
            .Distinct()
            .ToList();

        var foundClaims = await warrantyClaims
            .Where(x => !x.IsDeleted)
            .Where(x => claimNumbers.Contains(x.ClaimNumber + "-" + x.InvoiceNo))
            .ToListAsync();

        var exceptions = new List<ShiftEntityException>();

        foreach (var settlement in settlementClaims)
        {
            var claim = foundClaims
                .Where(x => x.ClaimNumber.Equals(settlement.ClaimNumber, StringComparison.InvariantCultureIgnoreCase))
                .Where(x => x.InvoiceNo != null && x.InvoiceNo.Equals(settlement.InvoiceNo, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

            if (claim == null)
            {
                if (manufacturerSettlmentSheet.PreventSubmittingUnrecognizedClaimNumbers)
                {
                    exceptions.Add(
                        new ShiftEntityException(
                            new Message($"Claim #{settlement.ClaimNumber} / Invoice #{settlement.InvoiceNo}", $"This claim is not available on the warranty system."),
                            additionalData: new() { ["Missing Claim"] = $"{settlement.ClaimNumber!}/{settlement.InvoiceNo}" }
                        )
                    );
                }

                continue;
            }

            //if (!string.IsNullOrWhiteSpace(claim.InvoiceNo) &&
            //    !claim.InvoiceNo.Equals(settlement.InvoiceNo, StringComparison.InvariantCultureIgnoreCase))
            //{
            //    exceptions.Add(
            //        new ShiftEntityException(
            //            new Message($"Claim #{settlement.ClaimNumber}", $"Invoice number is changed from ({claim.InvoiceNo}) to ({settlement.InvoiceNo})."),
            //            additionalData: new() { ["Invoice Number Changed"] = settlement.ClaimNumber! }
            //        )
            //    );

            //    continue;
            //}

            if (
                (claim.ManufacturerSettledLaborTotalAmountJPY != null && claim.ManufacturerSettledLaborTotalAmountJPY != settlement.SettledAmountLabor) ||
                (claim.ManufacturerSettledPartsTotalAmountJPY != null && claim.ManufacturerSettledPartsTotalAmountJPY != settlement.SettledAmountParts) ||
                (claim.ManufacturerSettledSubletTotalAmountJPY != null && claim.ManufacturerSettledSubletTotalAmountJPY != settlement.SettledAmountSublet)
            )
            {
                exceptions.Add(
                    new ShiftEntityException(
                        new Message($"Claim #{settlement.ClaimNumber}", $"Settled Amount for Labor, Parts, Sublet are changed from ({claim.ManufacturerSettledLaborTotalAmountJPY}, {claim.ManufacturerSettledPartsTotalAmountJPY}, {claim.ManufacturerSettledSubletTotalAmountJPY}) to ({settlement.SettledAmountLabor}, {settlement.SettledAmountParts}, {settlement.SettledAmountSublet})"),
                        additionalData: new() { ["Values Changed"] = settlement.ClaimNumber! }
                    )
                );

                continue;
            }

            claim.ManufacturerErrorMessage = null;

            if (settlement.AmountSettled == 0)
            {
                claim.ManufacturerStatus = WarrantyManufacturerClaimStatus.Rejected;

                claim.ManufacturerErrorMessage = $"{settlement.MainReasonCode}: {settlement.WahComment}";

                continue;
            }

            //claim.InvoiceNo = settlement.InvoiceNo;
            claim.ManufacturerSettlmentSheet = manufacturerSettlmentSheet;

            claim.ManufacturerSettledLaborTotalAmountJPY = settlement.SettledAmountLabor;
            claim.ManufacturerSettledPartsTotalAmountJPY = settlement.SettledAmountParts;
            claim.ManufacturerSettledSubletTotalAmountJPY = settlement.SettledAmountSublet;
            claim.ManufacturerStatus = WarrantyManufacturerClaimStatus.Paid;

            this.UpdateManufacturerAmounts(claim);
        }

        if (exceptions.Count > 0)
        {
            var errorLimit = 50;

            if (exceptions.Count > errorLimit)
            {
                var remainingErrorCountAfterTakingErrorLimit = exceptions.Count - errorLimit;

                exceptions = exceptions.Take(errorLimit).ToList();

                exceptions.Add(new ShiftEntityException(new Message($"+{remainingErrorCountAfterTakingErrorLimit} more claims".ToString(), $"omitted in this warning dialog")));
            }

            throw new ShiftEntityException(
                new Message(
                    "Error",
                    "",
                    exceptions.Select(x => x.Message).ToList()
                ),
                additionalData: new() { ["Exceptions"] = exceptions }
            );
        }
    }

    public async Task AutoGenerateFieldsAsync(IQueryable<WarrantyClaim> q, WarrantyClaim upserted, string dealerShortCode)
    {
        upserted.DealerCode = dealerShortCode;

        var autoGeneratedClaimNoStartDate = new DateTime(2025, 07, 22, 7, 0, 0, 0, DateTimeKind.Utc);

        var autoGeneratedClaimNumberStartDate = new DateTime(2025, 12, 02, 0, 0, 0, 0, DateTimeKind.Utc);

        if (upserted.CreateDate > autoGeneratedClaimNoStartDate)
        {
            var lastCompanyClaimNo = await q
                .Where(x => x.ID != 0)
                .Where(x => x.ID != upserted.ID)
                .Where(x => x.CompanyID == upserted.CompanyID)
                .Where(x => !x.IsDeleted)
                .Where(x => x.CreateDate > autoGeneratedClaimNoStartDate)
                .OrderByDescending(x => x.ID)
                .Select(x => x.DealerClaimNo)
                .FirstOrDefaultAsync();

            if (lastCompanyClaimNo is null)
            {
                upserted.DealerClaimNo = "1";
            }
            else
            {
                upserted.DealerClaimNo = (int.Parse(lastCompanyClaimNo) + 1).ToString();
            }
        }

        if (upserted.CreateDate > autoGeneratedClaimNumberStartDate)
        {
            var currentYear = upserted.CreateDate.Year;

            var lastClaimNumber = await q
                .Where(x => x.ID != 0)
                .Where(x => x.ID != upserted.ID)
                .Where(x => x.CompanyID == upserted.CompanyID)
                .Where(x => !x.IsDeleted)
                .Where(x => x.CreateDate > autoGeneratedClaimNumberStartDate)
                .Where(x => x.CreateDate.Year == currentYear)
                .OrderByDescending(x => x.ID)
                .Select(x => x.ClaimNumber)
                .FirstOrDefaultAsync();

            if (lastClaimNumber is null)
                lastClaimNumber = "0";
            else
                lastClaimNumber = lastClaimNumber.Substring(3); //Remove YearCode and DealerCode

            lastClaimNumber = (int.Parse(lastClaimNumber) + 1).ToString();

            var yearCode = currentYear.ToString().Last().ToString();

            var dealerCodeMapping = new Dictionary<long, string>
            {
                [1] = "SH",
                [9] = "1",
                [10] = "2",
                [6] = "4",
                [8] = "5",
                [5] = "7",
                [7] = "8",
                [3] = "99",
            };

            string? dealerClaimNumberCode = dealerCodeMapping.ContainsKey(upserted.CompanyID!.Value) ? dealerCodeMapping[upserted.CompanyID!.Value] : null;

            if (dealerClaimNumberCode is null)
                throw new ShiftEntityException(new Message("Error in Claim No Generation", "Your Dealer does not have a mapping for Claim No generation. Please contact system administrator."), additionalData: new Dictionary<string, object> { ["ErrorCode"] = "MissingCompanyClaimNumberPrefix" });

            lastClaimNumber = $"{yearCode}{dealerClaimNumberCode.PadLeft(2, '0')}{lastClaimNumber.PadLeft(4, '0')}";

            upserted.ClaimNumber = lastClaimNumber;
        }
    }
}
