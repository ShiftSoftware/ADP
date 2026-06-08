using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleSSCEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregateCosmosModel;

    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel)
    {
        this.CompanyDataAggregateCosmosModel = companyDataAggregateCosmosModel;
    }

    public IEnumerable<SscDTO> Evaluate()
    {
        var ssc = CompanyDataAggregateCosmosModel.SSCAffectedVINs;

        var warrantyClaims = CompanyDataAggregateCosmosModel.WarrantyClaims;

        var labors = CompanyDataAggregateCosmosModel.LaborLines;

        if (ssc?.Count() == 0)
            return null;

        var data = new List<SscDTO>();

        data = ssc?.Select(x =>
        {
            var parts = new List<SSCPartDTO>();
            var sscLabors = new List<SSCLaborDTO>();

            var isRepared = x.RepairDate is not null;
            DateTime? repairDate = x.RepairDate;

            var campaignCode = x.CampaignCode?.Trim();
            var laborCodes = new[] { x.LaborCode1, x.LaborCode2, x.LaborCode3 }
                .Select(c => c?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            var warrantyClaim = warrantyClaims?
                .Where(w => new List<ClaimStatus> { ClaimStatus.Accepted, ClaimStatus.Certified, ClaimStatus.Invoiced }.Contains(w?.ClaimStatus ?? 0))
                .OrderByDescending(w => w.RepairCompletionDate)
                .FirstOrDefault(w =>
                    (!string.IsNullOrEmpty(campaignCode) && (w.DistributorComment?.Contains(campaignCode) ?? false)) ||
                    (w.LaborLines?.Any(y => laborCodes.Contains(y.LaborCode?.Trim())) ?? false)
                );

            if (warrantyClaim is not null)
            {
                isRepared = true;
                repairDate = warrantyClaim.RepairCompletionDate;
            }
            else
            {
                var labor = labors?.OrderByDescending(s => s.InvoiceDate)
                    .FirstOrDefault(s =>
                        laborCodes.Contains(s.LaborCode?.Trim()) &&
                        (s.InvoiceStatus?.Trim() is "X" or "C")
                    );

                if (labor is not null)
                {
                    isRepared = true;
                    repairDate = labor.InvoiceDate;
                }
            }

            var sscData = new SscDTO
            {
                Description = x.Description,
                SSCCode = campaignCode,
                Repaired = isRepared,
                RepairDate = repairDate
            };

            if (!string.IsNullOrWhiteSpace(x.LaborCode1))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode1.Trim(),
                });

            if (!string.IsNullOrWhiteSpace(x.LaborCode2))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode2.Trim(),
                });

            if (!string.IsNullOrWhiteSpace(x.LaborCode3))
                sscLabors.Add(new SSCLaborDTO
                {
                    LaborCode = x.LaborCode3.Trim(),
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber1))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber1.Trim(),
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber2))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber2.Trim(),
                });

            if (!string.IsNullOrWhiteSpace(x.PartNumber3))
                parts.Add(new SSCPartDTO
                {
                    PartNumber = x.PartNumber3.Trim(),
                });

            sscData.Parts = parts;
            sscData.Labors = sscLabors;

            return sscData;
        }).ToList();

        // Get partnumbers and format it to match the stock item
        var partNumbers = data?.SelectMany(x => x.Parts.Select(p => p.PartNumber)).Distinct();

        return data;
    }
}