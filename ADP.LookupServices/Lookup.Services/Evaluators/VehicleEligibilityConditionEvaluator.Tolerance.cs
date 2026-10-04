using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal sealed partial class VehicleEligibilityConditionEvaluator
{
    // Null means the policy could not be applied. The caller decides how to handle the diagnostic.
    internal EligibilityConditionOutcome EvaluateTolerance(
        ServiceItemModel item, long? cap, DateTime? calculatedStart,
        ServiceRewardToleranceOptions policy, out string diagnostic)
    {
        diagnostic = null;
        if (policy is null || calculatedStart is null || calculatedStart >= policy.StartsBefore ||
            policy.ServiceItemIDs?.Contains(item.IntegrationID) != true ||
            item.ProgramRole != ServiceItemProgramRole.Reward)
            return null;

        var conditions = item.EligibilityConditions?.ToList() ?? new();
        var locks = conditions.Where(c => c?.WhenUnmet == EligibilityConditionUnmetBehavior.Lock).ToList();
        var ceilings = conditions.Where(c => c?.Field == ServiceHistoryMaximumMilestoneField).ToList();
        var requirement = locks.Count == 1 ? locks[0] : null;
        var ceiling = ceilings.Count == 1 ? ceilings[0] : null;
        var expected = new List<long>();
        if (cap is null || requirement is null || ceiling is null ||
            requirement.Field != ServiceHistoryPackageCodeField ||
            requirement.Operator != EligibilityConditionOperator.ContainsAll ||
            requirement.ValueMatch != EligibilityConditionValueMatch.Milestone ||
            requirement.Scope?.Selection != EligibilityConditionSelection.All || requirement.Scope.Count is not null ||
            requirement.Values is null || requirement.Values.Count() != 2 ||
            !TryReadMilestoneFilters(requirement, out var programs, out var qualifier) ||
            ceiling.Operator != EligibilityConditionOperator.Equals || ceiling.ValueMatch != EligibilityConditionValueMatch.Exact ||
            ceiling.WhenUnmet != EligibilityConditionUnmetBehavior.Miss || ceiling.Scope is not null ||
            ceiling.Values?.Count() != 1 || !TryParseMileage(ceiling.Values.Single(), out var maximum) ||
            !TryReadMilestoneFilters(ceiling, out var ceilingPrograms, out var ceilingQualifier) ||
            policy.TerminalPrograms is null || policy.TerminalPrograms.Count == 0 ||
            policy.TerminalPrograms.Any(string.IsNullOrWhiteSpace) ||
            !Enum.IsDefined(typeof(RewardHighPackageBehavior), policy.HighPackageBehavior))
        {
            diagnostic = "Historical visit policy requires a valid two-milestone reward and terminal programs.";
            return null;
        }
        foreach (var value in requirement.Values)
        {
            if (!TryParseMileage(value, out var mileage))
            {
                diagnostic = "Historical visit policy has an invalid prerequisite mileage.";
                return null;
            }
            expected.Add(mileage);
        }
        expected.Sort();
        if (expected[0] <= cap || expected[0] >= expected[1] || expected[1] != maximum || item.MaximumMileage <= maximum ||
            item.MaximumMileage is null)
        {
            diagnostic = "Historical visit policy does not match the reward's schedule and ceiling.";
            return null;
        }

        // Keep every other catalog condition, including Hide, and never change warranty evaluation.
        var other = Evaluate(conditions.Where(c => c != requirement && c != ceiling), cap);
        if (other.State == EligibilityConditionState.Hidden)
            return other;

        var lines = VehicleServiceHistoryEvaluator.GetInvoices(companyDataAggregate, ConsistencyLevels.Strong)
            .SelectMany(i => i.LaborLines).ToList();
        var jobs = lines.GroupBy(l => (l.VIN, l.CompanyID, l.BranchID, l.OrderDocumentNumber))
            .Select(g => g.OrderBy(l => l.InvoiceDate).ThenBy(l => l.InvoiceNumber, StringComparer.Ordinal)
                .ThenBy(l => l.LineID, StringComparer.Ordinal).ToList()).ToList();

        // A host may confirm a known source duplicate. All original lines remain evidence.
        if (policy.AreDuplicateJobs is not null)
        {
            for (var i = 0; i < jobs.Count; i++)
            for (var j = jobs.Count - 1; j > i; j--)
            {
                if (!policy.AreDuplicateJobs(jobs[i], jobs[j])) continue;
                jobs[i].AddRange(jobs[j]);
                jobs.RemoveAt(j);
            }
        }

        bool Matches(OrderLaborLineModel line, HashSet<string> allowed, MilestoneQualifierFilter filter, out long mileage)
        {
            var reading = string.IsNullOrWhiteSpace(line.PackageCode) ? null : milestoneResolver.Resolve(line.PackageCode);
            mileage = reading?.Milestone ?? 0;
            return reading is not null && (allowed is null || allowed.Contains(reading.Program ?? "")) &&
                filter.Accepts(reading.Qualifier);
        }
        bool Terminal(OrderLaborLineModel line)
        {
            var reading = string.IsNullOrWhiteSpace(line.PackageCode) ? null : milestoneResolver.Resolve(line.PackageCode);
            return reading?.Milestone == cap && policy.TerminalPrograms.Contains(reading.Program, StringComparer.OrdinalIgnoreCase);
        }
        bool HasIdentity(OrderLaborLineModel line) => !string.IsNullOrWhiteSpace(line.VIN) &&
            line.CompanyID is not null && line.BranchID is not null && !string.IsNullOrWhiteSpace(line.OrderDocumentNumber);

        var anchors = jobs.Where(j => j.Any(Terminal)).ToList();
        if (anchors.Count != 1 || anchors[0].Any(l => !HasIdentity(l) || l.InvoiceDate is null) ||
            anchors[0].Where(Terminal).Select(l => l.InvoiceDate).Distinct().Count() != 1)
        {
            diagnostic = "Historical visit policy needs one dated terminal service job; the anchor is missing or conflicting.";
            return null;
        }
        // The recognized terminal line dates the boundary, not a later claim or an ancillary invoice.
        var anchor = anchors[0].First(Terminal).InvoiceDate.Value;
        var returns = jobs.Where(j => j != anchors[0]).Select(j => new
        {
            Lines = j,
            Date = j.Min(l => l.InvoiceDate),
            High = j.Any(l => Matches(l, ceilingPrograms, ceilingQualifier, out var mileage) && mileage > maximum),
            Evidence = j.FirstOrDefault(l => Matches(l, programs, qualifier, out _) ||
                policy.IsAdditionalQualifyingWork?.Invoke(l) == true ||
                (policy.HighPackageBehavior == RewardHighPackageBehavior.CountInSequence &&
                 Matches(l, ceilingPrograms, ceilingQualifier, out var mileage) && mileage > maximum)),
        }).Where(j => j.Evidence is not null || j.High).ToList();
        if (returns.Any(j => j.Lines.Any(l => !HasIdentity(l) || l.InvoiceDate is null)))
        {
            diagnostic = "Historical visit policy has qualifying work without a dated job identity.";
            return null;
        }
        var sequence = returns.Where(j => j.Date > anchor).OrderBy(j => j.Date)
            .ThenBy(j => j.Lines[0].CompanyID).ThenBy(j => j.Lines[0].BranchID)
            .ThenBy(j => j.Lines[0].OrderDocumentNumber, StringComparer.Ordinal).ToList();
        var immediateMiss = policy.HighPackageBehavior == RewardHighPackageBehavior.ImmediateMiss && sequence.Any(j => j.High);
        var qualifying = sequence.Where(j => j.Evidence is not null &&
            (policy.HighPackageBehavior == RewardHighPackageBehavior.CountInSequence || !j.High)).ToList();

        var reached = new Dictionary<long, OrderLaborLineModel>();
        for (var i = 0; i < Math.Min(2, qualifying.Count); i++)
            reached.Add(expected[i], qualifying[i].Evidence);
        RecordPrerequisites(expected, reached);
        // SatisfiedOn is the first job invoice date; Evidence keeps the actual qualifying line date.
        for (var i = 0; i < Math.Min(2, qualifying.Count); i++)
            prerequisites[i].SatisfiedOn = qualifying[i].Date;

        var state = immediateMiss || qualifying.Count > 2 ? EligibilityConditionState.Missed :
            qualifying.Count < 2 ? EligibilityConditionState.Locked : other.State;
        diagnostic = $"Historical visit policy: {sequence.Count} return jobs, {policy.HighPackageBehavior}, last standard service {anchor:yyyy-MM-dd}.";
        return new EligibilityConditionOutcome(state, prerequisites, milestoneNearMisses,
            state == EligibilityConditionState.Met ? ResolveUnlockDate() : null);
    }
}
