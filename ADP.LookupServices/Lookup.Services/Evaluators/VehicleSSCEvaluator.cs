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

        var data = ssc?.Select(x =>
        {
            var isRepared = x.RepairDate is not null;
            DateTime? repairDate = x.RepairDate;

            var campaignCode = x.CampaignCode?.Trim();

            var laborCodes = x.EffectiveLabors
                .Select(l => l.LaborCode?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            var partNumbers = x.EffectivePartNumbers
                .Select(p => p?.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
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

            return new SscDTO
            {
                Description = x.Description,
                SSCCode = campaignCode,
                Repaired = isRepared,
                RepairDate = repairDate,
                Labors = laborCodes.Select(c => new SSCLaborDTO { LaborCode = c }).ToList(),
                Parts = partNumbers.Select(p => new SSCPartDTO { PartNumber = p }).ToList(),
            };
        }).ToList();

        return data;
    }
}