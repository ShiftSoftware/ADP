using AutoMapper;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Enums;
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
    private readonly IMapper? mapper;
    private readonly CosmosClient? cosmosClient;

    public WarrantyClaimService(IUserClaimService? userClaimService = null, IMapper? mapper = null, CosmosClient? cosmosClient = null)
    {
        this.userClaimService = userClaimService;
        this.mapper = mapper;
        this.cosmosClient = cosmosClient;
    }

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

                if (existingSubletLine?.DistributorAmount is null)
                    subletDto.DistributorAmount = subletDto.Amount;
                else
                    subletDto.DistributorAmount = existingSubletLine.DistributorAmount;
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

    private async Task<string?> FindSSCCampaignCodeAsync(List<WarrantyClaimLaborLineDTO> laborLines)
    {
        if (cosmosClient is null || laborLines.Count == 0)
            return null;

        var mainOperationNumbers = laborLines
            .Where(x => x.MainOperation && !string.IsNullOrWhiteSpace(x.OperationNumber))
            .Select(x => x.OperationNumber!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var otherOperationNumbers = laborLines
            .Where(x => !x.MainOperation && !string.IsNullOrWhiteSpace(x.OperationNumber))
            .Select(x => x.OperationNumber!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (mainOperationNumbers.Count == 0 && otherOperationNumbers.Count == 0)
            return null;

        var container = cosmosClient.GetContainer(
            NoSQLConstants.Databases.CompanyData,
            NoSQLConstants.Containers.Vehicles
        );

        var queryDefinition = new QueryDefinition(
            "SELECT c.CampaignCode, c.LaborCode1, c.LaborCode2, c.LaborCode3 FROM c WHERE c.ItemType = @itemType"
        )
        .WithParameter("@itemType", (string)ModelTypes.SSCAffectedVIN);

        var iterator = container.GetItemQueryIterator<SSCLaborCodeResult>(queryDefinition);

        string? fallback = null;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();

            foreach (var item in response)
            {
                var laborCodes = new[] { item.LaborCode1, item.LaborCode2, item.LaborCode3 }
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                if (laborCodes.Any(lc => mainOperationNumbers.Contains(lc)))
                    return item.CampaignCode;

                if (fallback is null && laborCodes.Any(lc => otherOperationNumbers.Contains(lc)))
                    fallback = item.CampaignCode;
            }
        }

        return fallback;
    }

    private class SSCLaborCodeResult
    {
        public string CampaignCode { get; set; } = default!;
        public string? LaborCode1 { get; set; }
        public string? LaborCode2 { get; set; }
        public string? LaborCode3 { get; set; }
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
                csvEntry.LaborHour3 = currentLaborItem.DistributorHour!.Value; // (int)(currentLaborItem.LaborHourTIQ * 1m);

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

        claim.LaborRateJPY = Math.Ceiling(claim.LaborRate * claim.LaborExchangeRate.Value);
        claim.WarrantyClaimSubletLines.ToList().ForEach(x => x.AmountJPY = Math.Ceiling(x.Amount * claim.SubletExchangeRate.Value));
        claim.WarrantyClaimSubletLines.ToList().ForEach(x => x.DistributorAmountJPY = Math.Ceiling((x.DistributorAmount ?? 0) * claim.SubletExchangeRate.Value));
        claim.WarrantyClaimPartLines.ToList().ForEach(x => x.DistributorPriceJPY = Math.Ceiling((x.DistributorPrice ?? 0) * claim.PartExchangeRate.Value));


        claim.LaborTotalAmountDistributor = claim.HourTotalDistributor * claim.LaborRate;
        claim.SubletTotalAmount = claim.WarrantyClaimSubletLines.Sum(x => x.Amount);
        claim.SubletTotalAmountDistributor = claim.WarrantyClaimSubletLines.Sum(x => x.DistributorAmount ?? 0m);
        claim.PartsTotalAmountDistributor = claim.WarrantyClaimPartLines.Sum(x => (x.DistributorPrice ?? 0m) * (x.Qty ?? 0m)) * claim.PRR1;


        claim.LaborTotalAmountDistributorJPY = claim.HourTotalDistributor * claim.LaborRateJPY;
        claim.SubletTotalAmountJPY = claim.WarrantyClaimSubletLines.Sum(x => x.AmountJPY);
        claim.SubletTotalAmountDistributorJPY = claim.WarrantyClaimSubletLines.Sum(x => x.DistributorAmountJPY ?? 0m);
        claim.PartsTotalAmountDistributorJPY = claim.WarrantyClaimPartLines.Sum(x => (x.DistributorPriceJPY ?? 0m) * (x.Qty ?? 0m)) * claim.PRR1.Value;

        claim.TotalClaimAmountDistributor = claim.LaborTotalAmountDistributor + claim.SubletTotalAmountDistributor + claim.PartsTotalAmountDistributor;
        claim.TotalClaimAmountDistributorJPY = claim.LaborTotalAmountDistributorJPY + claim.SubletTotalAmountDistributorJPY + claim.PartsTotalAmountDistributorJPY;

        this.UpdateManufacturerAmounts(claim);
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
