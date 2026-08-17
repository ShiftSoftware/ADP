using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Evaluates the closed declarative condition grammar shared by service-item and
/// extended-warranty eligibility. Unsupported fields or invalid condition shapes fail closed.
/// </summary>
internal sealed class VehicleEligibilityConditionEvaluator
{
    internal const string ServiceHistoryPackageCodeField = "serviceHistory.laborLines.packageCode";
    internal const string ServiceHistoryMaximumMilestoneField = "serviceHistory.laborLines.maximumMilestone";
    internal const string BaseScheduleMaximumMileageField = "serviceItems.baseSchedule.maximumMileage";

    private static readonly ServiceMilestoneOptions DefaultMilestoneOptions = new ServiceMilestoneOptions();

    private readonly CompanyDataAggregateModel companyDataAggregate;
    private readonly IServiceMilestoneResolver milestoneResolver;
    private readonly bool collectQualifierNearMisses;

    // Gathered per Evaluate call and reset by it, so one instance can be reused across definitions
    // without carrying the last one's findings into the next.
    private List<VehicleServiceItemPrerequisiteDTO> prerequisites;
    private List<ServiceItemMilestoneQualifierNearMiss> qualifierNearMisses;

    internal VehicleEligibilityConditionEvaluator(
        CompanyDataAggregateModel companyDataAggregate,
        LookupOptions options,
        bool collectQualifierNearMisses = false)
    {
        this.companyDataAggregate = companyDataAggregate;
        this.milestoneResolver = (options?.ServiceMilestones ?? DefaultMilestoneOptions).GetResolver();
        this.collectQualifierNearMisses = collectQualifierNearMisses;
    }

    internal bool MatchesAll(
        IEnumerable<EligibilityConditionModel> conditions,
        long? baseScheduleMaximumMileage = null) =>
        Evaluate(conditions, baseScheduleMaximumMileage).IsMet;

    /// <summary>
    /// Evaluates every condition and reports what the set decided. Unlike the per-condition
    /// predicates this does not stop at the first failure — except for a hiding one, which settles
    /// the matter outright — because the state an item ends up in depends on all of them, and
    /// because a locked card has to name prerequisites that the first failure alone cannot supply.
    /// </summary>
    internal EligibilityConditionOutcome Evaluate(
        IEnumerable<EligibilityConditionModel> conditions,
        long? baseScheduleMaximumMileage = null)
    {
        prerequisites = null;
        qualifierNearMisses = null;

        var state = EligibilityConditionState.Met;

        foreach (var condition in conditions ?? Enumerable.Empty<EligibilityConditionModel>())
        {
            if (condition is null)
                return EligibilityConditionOutcome.Hidden;

            if (MatchesCondition(condition, baseScheduleMaximumMileage))
                continue;

            switch (condition.WhenUnmet)
            {
                case EligibilityConditionUnmetBehavior.Hide:
                    return EligibilityConditionOutcome.Hidden;

                case EligibilityConditionUnmetBehavior.Lock:
                    state = EligibilityConditionState.Locked;
                    break;

                case EligibilityConditionUnmetBehavior.Miss:
                    // Locked outranks missed however the conditions are ordered: a customer who has
                    // not finished their prerequisites has missed nothing, even though the clause
                    // about going too far is failing at the same time — it fails on a null maximum.
                    if (state != EligibilityConditionState.Locked)
                        state = EligibilityConditionState.Missed;
                    break;

                default:
                    return EligibilityConditionOutcome.Hidden;
            }
        }

        if (state == EligibilityConditionState.Met)
            return EligibilityConditionOutcome.Met;

        return new EligibilityConditionOutcome(state, prerequisites, qualifierNearMisses);
    }

    private bool MatchesCondition(EligibilityConditionModel condition, long? baseScheduleMaximumMileage)
    {
        if (string.Equals(condition.Field, ServiceHistoryPackageCodeField, StringComparison.Ordinal))
            return MatchesServiceHistoryCondition(condition);

        if (string.Equals(condition.Field, ServiceHistoryMaximumMilestoneField, StringComparison.Ordinal))
            return MatchesMaximumMilestoneCondition(condition);

        if (string.Equals(condition.Field, BaseScheduleMaximumMileageField, StringComparison.Ordinal))
            return MatchesBaseScheduleMaximumMileageCondition(condition, baseScheduleMaximumMileage);

        return false;
    }

    private bool MatchesServiceHistoryCondition(EligibilityConditionModel condition)
    {
        var requiredValues = condition.Values?.ToList();
        if (condition.Operator != EligibilityConditionOperator.ContainsAll ||
            condition.Scope is null ||
            requiredValues is null ||
            requiredValues.Count == 0 ||
            requiredValues.Any(string.IsNullOrWhiteSpace))
            return false;

        if (condition.ValueMatch == EligibilityConditionValueMatch.Milestone)
            return MatchesMilestonePackageCodeCondition(condition, requiredValues);

        // The text comparisons know nothing about programmes or qualifiers — those are read out of a
        // milestone, and there is no milestone here. An author who wrote one meant something this
        // comparison would silently ignore, which is the shape of mistake the grammar fails closed on.
        if ((condition.ValueMatch != EligibilityConditionValueMatch.Exact &&
                condition.ValueMatch != EligibilityConditionValueMatch.EndsWith) ||
            condition.Program is not null ||
            condition.Qualifier is not null)
            return false;

        var invoices = SelectInvoices(condition.Scope);
        if (invoices is null)
            return false;

        var packageCodes = invoices
            .SelectMany(invoice => invoice.LaborLines ?? Enumerable.Empty<OrderLaborLineModel>())
            .Select(line => line.PackageCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList();

        return requiredValues.All(value =>
            packageCodes.Any(code => MatchesValue(code, value, condition.ValueMatch)));
    }

    /// <summary>
    /// "Has this milestone ever been reached", asked of each configured mileage in turn. Unscheduled
    /// work carries no milestone, so it cannot answer either way — which is what matching numerically
    /// buys over selecting a window of recent invoices and hoping the right ones are still in it.
    /// </summary>
    private bool MatchesMilestonePackageCodeCondition(
        EligibilityConditionModel condition,
        List<string> requiredValues)
    {
        if (!TryReadMilestoneFilters(condition, out var programs, out var qualifier))
            return false;

        var requiredMilestones = new List<long>();
        foreach (var value in requiredValues)
        {
            if (!TryParseMileage(value, out var milestone))
                return false;
            requiredMilestones.Add(milestone);
        }

        var invoices = SelectInvoices(condition.Scope);
        if (invoices is null)
            return false;

        var reached = CollectMilestones(invoices, programs, qualifier);

        // Only a locking clause names prerequisites. The ceiling a disqualifier compares against is
        // not a step the customer takes, and listing it on the card would read as one more service
        // they still owe.
        if (condition.WhenUnmet == EligibilityConditionUnmetBehavior.Lock)
            RecordPrerequisites(requiredMilestones, reached);

        return requiredMilestones.All(reached.ContainsKey);
    }

    private void RecordPrerequisites(List<long> requiredMilestones, Dictionary<long, DateTime?> reached)
    {
        prerequisites = prerequisites ?? new List<VehicleServiceItemPrerequisiteDTO>();

        foreach (var milestone in requiredMilestones)
        {
            var satisfied = reached.TryGetValue(milestone, out var satisfiedOn);

            prerequisites.Add(new VehicleServiceItemPrerequisiteDTO
            {
                Mileage = milestone,
                Label = FormatMilestoneLabel(milestone),
                Satisfied = satisfied,
                SatisfiedOn = satisfied ? satisfiedOn : null,
            });
        }
    }

    /// <summary>
    /// The mileage written the way milestones are written. Whole thousands become "45K", which is
    /// how the service is named everywhere it appears — on the code, on the invoice, and in what a
    /// customer would say out loud. Anything else is left as the number, because inventing a
    /// shorthand for it would be worse than showing the mileage.
    /// </summary>
    private static string FormatMilestoneLabel(long mileage) =>
        mileage % 1000 == 0
            ? (mileage / 1000).ToString(CultureInfo.InvariantCulture) + "K"
            : mileage.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The highest milestone the vehicle has reached, compared for equality. Paired with a
    /// prerequisite clause it says "and no further", which is what separates a customer still inside
    /// a window from one who has already gone past it — a distinction a single "has it happened"
    /// predicate cannot draw.
    /// <para>
    /// A vehicle with no milestone in its history has no maximum. That is not zero and not a match:
    /// a vehicle that has not started must be told apart from one that has finished.
    /// </para>
    /// </summary>
    private bool MatchesMaximumMilestoneCondition(EligibilityConditionModel condition)
    {
        var values = condition.Values?.ToList();
        if (condition.Operator != EligibilityConditionOperator.Equals ||
            condition.ValueMatch != EligibilityConditionValueMatch.Exact ||
            condition.Scope is not null ||
            values is not { Count: 1 } ||
            !TryParseMileage(values[0], out var requiredMilestone))
            return false;

        if (!TryReadMilestoneFilters(condition, out var programs, out var qualifier))
            return false;

        // No scope to select with: the highest milestone is a fact about the whole history, and a
        // window over it would answer a different question.
        var invoices = VehicleServiceHistoryEvaluator
            .GetInvoices(companyDataAggregate, ConsistencyLevels.Strong)
            .ToList();

        var reached = CollectMilestones(invoices, programs, qualifier);
        return reached.Count > 0 && reached.Keys.Max() == requiredMilestone;
    }

    /// <summary>
    /// The programme and qualifier filters a milestone condition applies, or false when either is a
    /// shape the grammar does not support.
    /// </summary>
    private static bool TryReadMilestoneFilters(
        EligibilityConditionModel condition,
        out HashSet<string> programs,
        out MilestoneQualifierFilter qualifier)
    {
        qualifier = null;

        if (!TryReadPrograms(condition.Program, out programs))
            return false;

        qualifier = MilestoneQualifierFilter.Read(condition.Qualifier);
        return qualifier is not null;
    }

    /// <summary>
    /// The programmes whose codes count, or null for every programme. Present but empty, or holding a
    /// blank entry, is an authoring mistake rather than a way of saying "all".
    /// </summary>
    private static bool TryReadPrograms(IEnumerable<string> configured, out HashSet<string> programs)
    {
        programs = null;

        if (configured is null)
            return true;

        var values = configured.ToList();
        if (values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
            return false;

        programs = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
        return true;
    }

    /// <summary>
    /// Every milestone the selected invoices reached, once each, with the date it was first
    /// recorded, after the programme and qualifier filters. Deduplicated because a milestone
    /// serviced twice, or split across two invoices, is one milestone reached — the number of lines
    /// carries no meaning here, and the earliest date is when the customer actually did it.
    /// </summary>
    private Dictionary<long, DateTime?> CollectMilestones(
        IEnumerable<VehicleServiceHistoryEvaluator.VehicleServiceHistoryInvoice> invoices,
        HashSet<string> programs,
        MilestoneQualifierFilter qualifier)
    {
        var reached = new Dictionary<long, DateTime?>();

        foreach (var line in invoices.SelectMany(invoice =>
                     invoice.LaborLines ?? Enumerable.Empty<OrderLaborLineModel>()))
        {
            var reading = milestoneResolver.Resolve(line?.PackageCode);
            if (reading is null)
                continue;

            if (programs is not null &&
                (reading.Program is null || !programs.Contains(reading.Program)))
                continue;

            if (!qualifier.Accepts(reading.Qualifier))
            {
                RecordQualifierNearMiss(reading);
                continue;
            }

            var performedOn = line?.InvoiceDate;

            if (!reached.TryGetValue(reading.Milestone, out var firstOn))
                reached[reading.Milestone] = performedOn;
            else if (performedOn is not null && (firstOn is null || performedOn < firstOn))
                reached[reading.Milestone] = performedOn;
        }

        return reached;
    }

    /// <summary>
    /// A code that named a milestone this rule wanted and was dropped on its trailing qualifier
    /// alone. Whether those services should have counted is a question about how a deployment books
    /// its work, which no amount of reading the catalog can settle — but counting the near misses
    /// turns it into a measurement.
    /// </summary>
    private void RecordQualifierNearMiss(ServiceMilestoneReading reading)
    {
        if (!collectQualifierNearMisses)
            return;

        qualifierNearMisses = qualifierNearMisses ?? new List<ServiceItemMilestoneQualifierNearMiss>();

        qualifierNearMisses.Add(new ServiceItemMilestoneQualifierNearMiss
        {
            Milestone = reading.Milestone,
            Program = reading.Program,
            Qualifier = reading.Qualifier,
        });
    }

    /// <summary>
    /// The invoices a scope selects, or null when the scope is not a shape this evaluator supports.
    /// Null is a closed door, not an empty selection: an unsupported scope must fail the condition
    /// rather than compare against nothing and pass.
    /// </summary>
    private List<VehicleServiceHistoryEvaluator.VehicleServiceHistoryInvoice> SelectInvoices(EligibilityConditionScope scope)
    {
        var invoices = VehicleServiceHistoryEvaluator.GetInvoices(companyDataAggregate, ConsistencyLevels.Strong);

        // A Count here is an authoring mistake rather than a harmless extra: All already takes the
        // whole history, so a number means the author had some other selection in mind.
        if (scope.Selection == EligibilityConditionSelection.All)
            return scope.Count is null ? invoices.ToList() : null;

        if (scope.Selection != EligibilityConditionSelection.Latest ||
            scope.Count is null ||
            scope.Count <= 0)
            return null;

        var latest = invoices
            .Select(invoice => new
            {
                Invoice = invoice,
                ServiceDate = new[]
                {
                    invoice.LaborLines?.Max(line => line.InvoiceDate),
                    invoice.PartLines?.Max(line => line.InvoiceDate),
                }.Max()
            })
            .OrderByDescending(x => x.ServiceDate)
            .Take(scope.Count.Value)
            .Select(x => x.Invoice)
            .ToList();

        // Too little history to fill the window means the window cannot be judged either way.
        return latest.Count == scope.Count.Value ? latest : null;
    }

    private static bool MatchesBaseScheduleMaximumMileageCondition(
        EligibilityConditionModel condition,
        long? baseScheduleMaximumMileage)
    {
        var values = condition.Values?.ToList();
        return condition.Operator == EligibilityConditionOperator.Equals &&
            condition.ValueMatch == EligibilityConditionValueMatch.Exact &&
            condition.Scope is null &&
            condition.Program is null &&
            condition.Qualifier is null &&
            values is { Count: 1 } &&
            long.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var requiredMileage) &&
            requiredMileage > 0 &&
            baseScheduleMaximumMileage == requiredMileage;
    }

    /// <summary>
    /// A configured mileage. <see cref="NumberStyles.None"/> takes plain digits only, so a sign, a
    /// thousands separator or surrounding space is rejected rather than quietly reinterpreted, and a
    /// value too large for the type fails to parse at all.
    /// </summary>
    private static bool TryParseMileage(string value, out long mileage) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out mileage) &&
        mileage > 0;

    private static bool MatchesValue(
        string actual,
        string required,
        EligibilityConditionValueMatch valueMatch) =>
        valueMatch switch
        {
            EligibilityConditionValueMatch.Exact =>
                string.Equals(actual, required, StringComparison.OrdinalIgnoreCase),
            EligibilityConditionValueMatch.EndsWith =>
                actual.EndsWith(required, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

    /// <summary>
    /// A validated <see cref="EligibilityConditionQualifier"/>. Reading it once, up front, keeps the
    /// question of which shapes are legal in one place and out of the per-line loop.
    /// </summary>
    private sealed class MilestoneQualifierFilter
    {
        private readonly EligibilityConditionQualifierSelection selection;
        private readonly HashSet<string> values;

        private MilestoneQualifierFilter(
            EligibilityConditionQualifierSelection selection,
            HashSet<string> values)
        {
            this.selection = selection;
            this.values = values;
        }

        /// <summary>
        /// The filter a condition configures, or null when it configures none or configures one this
        /// evaluator does not support. A milestone condition without a qualifier is unsupported
        /// deliberately: whether a variant-qualified code records the same service is a decision, and
        /// a default would make that decision silently on the author's behalf.
        /// </summary>
        internal static MilestoneQualifierFilter Read(EligibilityConditionQualifier configured)
        {
            if (configured is null)
                return null;

            var values = configured.Values?.ToList();

            switch (configured.Selection)
            {
                case EligibilityConditionQualifierSelection.None:
                case EligibilityConditionQualifierSelection.Any:
                    // These name no qualifiers, so a list is an author saying something the selection
                    // cannot carry.
                    return values is null
                        ? new MilestoneQualifierFilter(configured.Selection, null)
                        : null;

                case EligibilityConditionQualifierSelection.Only:
                case EligibilityConditionQualifierSelection.Except:
                    return values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace)
                        ? null
                        : new MilestoneQualifierFilter(
                            configured.Selection,
                            new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));

                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether a code carrying this qualifier — null when it carries none — takes part.
        /// </summary>
        internal bool Accepts(string qualifier) =>
            selection switch
            {
                EligibilityConditionQualifierSelection.None => qualifier is null,
                EligibilityConditionQualifierSelection.Any => true,
                EligibilityConditionQualifierSelection.Only => qualifier is not null && values.Contains(qualifier),
                EligibilityConditionQualifierSelection.Except => qualifier is null || !values.Contains(qualifier),
                _ => false,
            };
    }
}
