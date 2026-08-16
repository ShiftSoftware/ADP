using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Enums;
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
    internal const string BaseScheduleMaximumMileageField = "serviceItems.baseSchedule.maximumMileage";

    private readonly CompanyDataAggregateModel companyDataAggregate;

    internal VehicleEligibilityConditionEvaluator(CompanyDataAggregateModel companyDataAggregate)
    {
        this.companyDataAggregate = companyDataAggregate;
    }

    internal bool MatchesAll(
        IEnumerable<EligibilityConditionModel> conditions,
        long? baseScheduleMaximumMileage = null)
    {
        foreach (var condition in conditions ?? Enumerable.Empty<EligibilityConditionModel>())
        {
            if (condition is null)
                return false;

            if (string.Equals(condition.Field, ServiceHistoryPackageCodeField, StringComparison.Ordinal))
            {
                if (!MatchesServiceHistoryCondition(condition))
                    return false;
                continue;
            }

            if (string.Equals(condition.Field, BaseScheduleMaximumMileageField, StringComparison.Ordinal))
            {
                if (!MatchesBaseScheduleMaximumMileageCondition(condition, baseScheduleMaximumMileage))
                    return false;
                continue;
            }

            return false;
        }

        return true;
    }

    private bool MatchesServiceHistoryCondition(EligibilityConditionModel condition)
    {
        var requiredValues = condition.Values?.ToList();
        if (condition.Operator != EligibilityConditionOperator.ContainsAll ||
            (condition.ValueMatch != EligibilityConditionValueMatch.Exact &&
                condition.ValueMatch != EligibilityConditionValueMatch.EndsWith) ||
            condition.Scope is null ||
            requiredValues is null ||
            requiredValues.Count == 0 ||
            requiredValues.Any(string.IsNullOrWhiteSpace))
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
            values is { Count: 1 } &&
            long.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var requiredMileage) &&
            requiredMileage > 0 &&
            baseScheduleMaximumMileage == requiredMileage;
    }

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
}
