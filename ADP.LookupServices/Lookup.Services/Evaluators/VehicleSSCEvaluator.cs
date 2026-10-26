using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Decides, for every Special Service Campaign (SSC) / safety recall on a vehicle, whether the campaign's repair
/// has been carried out, and optionally records how that decision was reached.
/// <para>Three sources are consulted in order, and the first that holds decides the verdict and the repair date:</para>
/// <list type="number">
/// <item>The SSC record's own repair date.</item>
/// <item>The most recently completed warranty claim in a qualifying status
/// (<see cref="LookupOptions.SSCQualifyingWarrantyClaimStatuses"/>; Accepted, Certified, Invoiced by default) that
/// references the campaign — the campaign code appears in the distributor comment, or a claim labor line carries
/// one of the campaign's labor operation codes.</item>
/// <item>The most recent finished service-history labor line carrying one of the campaign's labor operation codes.
/// A line is finished when <see cref="LookupOptions.SSCQualifyingLaborLine"/> says so (by default, invoice status
/// X or C).</item>
/// </list>
/// <para>A deployment that sets <see cref="LookupOptions.PreferSSCRepairEvidenceOverRecordDate"/> consults the SSC
/// record last: its date decides only when there is no qualifying claim or line.</para>
/// <para>A labor code matches directly, or through <see cref="LookupOptions.SSCInterchangeableLaborCodeGroups"/>:
/// dealer systems sometimes book a campaign under a sibling operation code, and a deployment declares which
/// codes are used interchangeably so those repairs are still recognised.</para>
/// <para>The claim or line that decides the verdict also names the dealer and branch that carried out the repair
/// (<see cref="SscDTO.RepairCompanyID"/>, <see cref="SscDTO.RepairBranchID"/>). The SSC record names neither.</para>
/// </summary>
public class VehicleSSCEvaluator
{
    private readonly CompanyDataAggregateModel aggregate;
    private readonly LookupOptions options;
    private readonly IServiceProvider serviceProvider;
    private readonly SSCLaborCodeEquivalence equivalence;
    private readonly HashSet<ClaimStatus> qualifyingClaimStatuses;
    private readonly Func<OrderLaborLineModel, bool> qualifyingLaborLine;
    private readonly bool evidenceBeforeRecordDate;

    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel)
        : this(companyDataAggregateCosmosModel, null)
    {
    }

    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel, LookupOptions lookupOptions)
        : this(companyDataAggregateCosmosModel, lookupOptions, null)
    {
    }

    /// <param name="serviceProvider">Passed to the host's name resolvers by <see cref="EvaluateAsync"/>.</param>
    public VehicleSSCEvaluator(CompanyDataAggregateModel companyDataAggregateCosmosModel, LookupOptions lookupOptions, IServiceProvider serviceProvider)
    {
        this.aggregate = companyDataAggregateCosmosModel;
        this.options = lookupOptions;
        this.serviceProvider = serviceProvider;
        this.equivalence = lookupOptions?.SSCInterchangeableLaborCodeGroups is { Count: > 0 } groups
            ? new SSCLaborCodeEquivalence(groups)
            : SSCLaborCodeEquivalence.Empty;
        this.qualifyingClaimStatuses = new HashSet<ClaimStatus>(
            lookupOptions?.SSCQualifyingWarrantyClaimStatuses ?? LookupOptions.DefaultSSCQualifyingWarrantyClaimStatuses);
        this.qualifyingLaborLine = lookupOptions?.SSCQualifyingLaborLine ?? LookupOptions.DefaultSSCQualifyingLaborLine;
        this.evidenceBeforeRecordDate = lookupOptions?.PreferSSCRepairEvidenceOverRecordDate ?? false;
    }

    public IEnumerable<SscDTO> Evaluate() => Evaluate(includeTrace: false);

    /// <param name="includeTrace">
    /// When true, every <see cref="SscDTO"/> carries a <see cref="SscDTO.Trace"/> describing the evidence behind
    /// its verdict. Off by default: the trace lists claim and invoice details and is meant for permission-gated
    /// diagnostics, not the general lookup response.
    /// </param>
    /// <remarks>The repairing dealer and branch carry their ids only. <see cref="EvaluateAsync"/> also resolves their names.</remarks>
    public IEnumerable<SscDTO> Evaluate(bool includeTrace) => EvaluateAll(includeTrace)?.Select(x => x.Ssc).ToList();

    /// <summary>
    /// <see cref="Evaluate(bool)"/>, and then the names of the repairing dealer and branch, through the host's
    /// <see cref="LookupOptions.CompanyNameResolver"/> and <see cref="LookupOptions.CompanyBranchNameResolver"/>.
    /// A resolver is called only for a campaign whose evidence names a dealer or a branch.
    /// </summary>
    public async Task<IEnumerable<SscDTO>> EvaluateAsync(bool includeTrace, string languageCode)
    {
        var evaluated = EvaluateAll(includeTrace);

        if (evaluated is null)
            return null;

        foreach (var (ssc, repairCompanyID, repairBranchID) in evaluated)
        {
            if (repairCompanyID is not null && options?.CompanyNameResolver is not null)
                ssc.RepairCompanyName = await options.CompanyNameResolver(new(repairCompanyID, languageCode, serviceProvider));

            if (repairBranchID is not null && options?.CompanyBranchNameResolver is not null)
                ssc.RepairBranchName = await options.CompanyBranchNameResolver(new(repairBranchID, languageCode, serviceProvider));
        }

        return evaluated.Select(x => x.Ssc).ToList();
    }

    private List<(SscDTO Ssc, long? RepairCompanyID, long? RepairBranchID)> EvaluateAll(bool includeTrace)
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

    private (SscDTO Ssc, long? RepairCompanyID, long? RepairBranchID) EvaluateOne(
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
            var statusQualifies = qualifyingClaimStatuses.Contains(claim.ClaimStatus);

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

        // --- Service history: the latest finished labor line carrying one of the accepted codes. Only consulted
        // for the verdict when no claim was selected, but always walked when tracing so the reader can see the
        // lines that would have counted.
        var lineTraces = new List<SscRepairTraceLaborLineDTO>();
        OrderLaborLineModel selectedLine = null;

        foreach (var line in laborLines)
        {
            var key = SSCLaborCodeEquivalence.Normalize(line.LaborCode);

            if (key.Length == 0 || !acceptedCodes.TryGetValue(key, out var campaignLaborCode))
                continue;

            var statusQualifies = qualifyingLaborLine(line);
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
                    OrderStatus = line.OrderStatus,
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

        // --- Verdict: first source that holds, in declared order. When the deployment puts the evidence first, the
        // SSC record's own date decides only when there is no qualifying claim or line.
        var repairSource = SscRepairSource.None;
        DateTime? repairDate = null;
        long? repairCompanyID = null;
        long? repairBranchID = null;

        var recordDecides = ssc.RepairDate is not null
            && (!evidenceBeforeRecordDate || (selectedClaim is null && selectedLine is null));

        if (recordDecides)
        {
            repairSource = SscRepairSource.SSCRecord;
            repairDate = ssc.RepairDate;
        }
        else if (selectedClaim is not null)
        {
            repairSource = SscRepairSource.WarrantyClaim;
            repairDate = selectedClaim.RepairCompletionDate;
            repairCompanyID = selectedClaim.CompanyID;
            repairBranchID = selectedClaim.BranchID;
        }
        else if (selectedLine is not null)
        {
            repairSource = SscRepairSource.ServiceHistory;
            repairDate = selectedLine.InvoiceDate;
            repairCompanyID = selectedLine.CompanyID;
            repairBranchID = selectedLine.BranchID;
        }

        var result = new SscDTO
        {
            Description = ssc.Description,
            SSCCode = campaignCode,
            Repaired = repairSource != SscRepairSource.None,
            RepairDate = repairDate,
            RepairSource = repairSource,
            // The plain ids, like the sale information's; they are hashed only when the DTO is serialized.
            RepairCompanyID = repairCompanyID?.ToString(),
            RepairBranchID = repairBranchID?.ToString(),
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

        return (result, repairCompanyID, repairBranchID);
    }
}
