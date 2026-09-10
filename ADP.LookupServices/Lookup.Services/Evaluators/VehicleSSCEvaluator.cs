using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Decides, for every Special Service Campaign (SSC) / safety recall on a vehicle, whether the campaign's repair
/// has been carried out, and optionally records how that decision was reached.
/// <para>Three sources are consulted in order, and the first that holds decides the verdict and the repair date:</para>
/// <list type="number">
/// <item>The SSC record's own repair date.</item>
/// <item>The most recently completed warranty claim in a qualifying status (Accepted, Certified, Invoiced) that
/// references the campaign — the campaign code appears in the distributor comment, or a claim labor line carries
/// one of the campaign's labor operation codes.</item>
/// <item>The most recent invoiced service-history labor line (invoice status X or C) carrying one of the
/// campaign's labor operation codes.</item>
/// </list>
/// <para>A labor code matches directly, or through <see cref="LookupOptions.SSCInterchangeableLaborCodeGroups"/>:
/// dealer systems sometimes book a campaign under a sibling operation code, and a deployment declares which
/// codes are used interchangeably so those repairs are still recognised.</para>
/// </summary>
public class VehicleSSCEvaluator
{
    private static readonly ClaimStatus[] QualifyingClaimStatuses = { ClaimStatus.Accepted, ClaimStatus.Certified, ClaimStatus.Invoiced };
    private static readonly string[] QualifyingInvoiceStatuses = { "X", "C" };

    private readonly CompanyDataAggregateModel aggregate;
    private readonly SSCLaborCodeEquivalence equivalence;

    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel)
        : this(companyDataAggregateCosmosModel, null)
    {
    }

    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel, LookupOptions lookupOptions)
    {
        this.aggregate = companyDataAggregateCosmosModel;
        this.equivalence = lookupOptions?.SSCInterchangeableLaborCodeGroups is { Count: > 0 } groups
            ? new SSCLaborCodeEquivalence(groups)
            : SSCLaborCodeEquivalence.Empty;
    }

    public IEnumerable<SscDTO> Evaluate() => Evaluate(includeTrace: false);

    /// <param name="includeTrace">
    /// When true, every <see cref="SscDTO"/> carries a <see cref="SscDTO.Trace"/> describing the evidence behind
    /// its verdict. Off by default: the trace lists claim and invoice details and is meant for permission-gated
    /// diagnostics, not the general lookup response.
    /// </param>
    public IEnumerable<SscDTO> Evaluate(bool includeTrace)
    {
        var sscs = aggregate.SSCAffectedVINs;

        if (sscs?.Count() == 0)
            return null;

        var warrantyClaims = (aggregate.WarrantyClaims ?? Enumerable.Empty<WarrantyClaimModel>())
            .Where(w => w is not null)
            .OrderByDescending(w => w.RepairCompletionDate)
            .ToList();

        var laborLines = (aggregate.LaborLines ?? Enumerable.Empty<OrderLaborLineModel>())
            .Where(l => l is not null)
            .OrderByDescending(l => l.InvoiceDate)
            .ToList();

        return sscs?.Select(ssc => EvaluateOne(ssc, warrantyClaims, laborLines, includeTrace)).ToList();
    }

    private SscDTO EvaluateOne(
        SSCAffectedVINModel ssc,
        List<WarrantyClaimModel> warrantyClaims,
        List<OrderLaborLineModel> laborLines,
        bool includeTrace)
    {
        var campaignCode = ssc.CampaignCode?.Trim();

        var campaignLaborCodes = ssc.EffectiveLabors
            .Select(l => l.LaborCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        var partNumbers = ssc.EffectivePartNumbers
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        // Every code that counts as this campaign's work, keyed by its matching form and pointing back at the
        // campaign code it stands for. A campaign's own code always stands for itself, even if some configured
        // group would also make it an alternative of another of the campaign's codes.
        var acceptedCodes = new Dictionary<string, string>(StringComparer.Ordinal);
        var interchangeable = new List<SscRepairTraceLaborCodeDTO>();

        foreach (var code in campaignLaborCodes)
            acceptedCodes[SSCLaborCodeEquivalence.Normalize(code)] = code;

        foreach (var code in campaignLaborCodes)
        {
            foreach (var alternative in equivalence.AlternativesFor(code))
            {
                if (acceptedCodes.ContainsKey(alternative))
                    continue;

                acceptedCodes[alternative] = code;
                interchangeable.Add(new SscRepairTraceLaborCodeDTO { LaborCode = alternative, CampaignLaborCode = code });
            }
        }

        // --- Warranty claims: the latest completed claim that qualifies by status and references the campaign.
        var claimTraces = new List<SscRepairTraceWarrantyClaimDTO>();
        WarrantyClaimModel selectedClaim = null;

        foreach (var claim in warrantyClaims)
        {
            var statusQualifies = QualifyingClaimStatuses.Contains(claim.ClaimStatus);

            var campaignCodeInComment =
                !string.IsNullOrEmpty(campaignCode) && (claim.DistributorComment?.Contains(campaignCode) ?? false);

            var matchedCodes = (claim.LaborLines ?? Enumerable.Empty<WarrantyClaimLaborLineModel>())
                .Select(l => l?.LaborCode)
                .Select(code => (code: code?.Trim(), key: SSCLaborCodeEquivalence.Normalize(code)))
                .Where(x => x.key.Length > 0 && acceptedCodes.ContainsKey(x.key))
                .GroupBy(x => x.key)
                .Select(g => new SscRepairTraceLaborCodeDTO { LaborCode = g.First().code, CampaignLaborCode = acceptedCodes[g.Key] })
                .ToList();

            var matches = campaignCodeInComment || matchedCodes.Count > 0;
            var selected = selectedClaim is null && statusQualifies && matches;

            if (selected)
                selectedClaim = claim;

            if (includeTrace)
            {
                claimTraces.Add(new SscRepairTraceWarrantyClaimDTO
                {
                    ClaimNumber = claim.ClaimNumber,
                    DealerClaimNumber = claim.DealerClaimNumber,
                    ClaimStatus = claim.ClaimStatus,
                    RepairCompletionDate = claim.RepairCompletionDate,
                    StatusQualifies = statusQualifies,
                    CampaignCodeInComment = campaignCodeInComment,
                    DistributorComment = claim.DistributorComment,
                    MatchedLaborCodes = matchedCodes,
                    Matches = matches,
                    Selected = selected,
                });
            }
            else if (selectedClaim is not null)
            {
                break;
            }
        }

        // --- Service history: the latest invoiced labor line carrying one of the accepted codes. Only consulted
        // for the verdict when no claim was selected, but always walked when tracing so the reader can see the
        // lines that would have counted.
        var lineTraces = new List<SscRepairTraceLaborLineDTO>();
        OrderLaborLineModel selectedLine = null;

        foreach (var line in laborLines)
        {
            var key = SSCLaborCodeEquivalence.Normalize(line.LaborCode);

            if (key.Length == 0 || !acceptedCodes.TryGetValue(key, out var campaignLaborCode))
                continue;

            var statusQualifies = QualifyingInvoiceStatuses.Contains(line.InvoiceStatus?.Trim());
            var selected = selectedClaim is null && selectedLine is null && statusQualifies;

            if (selected)
                selectedLine = line;

            if (includeTrace)
            {
                lineTraces.Add(new SscRepairTraceLaborLineDTO
                {
                    InvoiceNumber = line.InvoiceNumber,
                    InvoiceDate = line.InvoiceDate,
                    InvoiceStatus = line.InvoiceStatus,
                    LaborCode = line.LaborCode?.Trim(),
                    CampaignLaborCode = campaignLaborCode,
                    StatusQualifies = statusQualifies,
                    Selected = selected,
                });
            }
            else if (selectedClaim is not null || selectedLine is not null)
            {
                break;
            }
        }

        // --- Verdict: first source that holds, in declared order.
        var repairSource = SscRepairSource.None;
        DateTime? repairDate = null;

        if (ssc.RepairDate is not null)
        {
            repairSource = SscRepairSource.SSCRecord;
            repairDate = ssc.RepairDate;
        }
        else if (selectedClaim is not null)
        {
            repairSource = SscRepairSource.WarrantyClaim;
            repairDate = selectedClaim.RepairCompletionDate;
        }
        else if (selectedLine is not null)
        {
            repairSource = SscRepairSource.ServiceHistory;
            repairDate = selectedLine.InvoiceDate;
        }

        return new SscDTO
        {
            Description = ssc.Description,
            SSCCode = campaignCode,
            Repaired = repairSource != SscRepairSource.None,
            RepairDate = repairDate,
            RepairSource = repairSource,
            Labors = campaignLaborCodes.Select(c => new SSCLaborDTO { LaborCode = c }).ToList(),
            Parts = partNumbers.Select(p => new SSCPartDTO { PartNumber = p }).ToList(),
            Trace = includeTrace
                ? new SscRepairTraceDTO
                {
                    CampaignLaborCodes = campaignLaborCodes,
                    InterchangeableLaborCodes = interchangeable,
                    RecordRepairDate = ssc.RepairDate,
                    WarrantyClaims = claimTraces,
                    ServiceHistoryLaborLinesExamined = laborLines.Count,
                    ServiceHistoryLaborLines = lineTraces,
                    RepairSource = repairSource,
                }
                : null,
        };
    }
}
