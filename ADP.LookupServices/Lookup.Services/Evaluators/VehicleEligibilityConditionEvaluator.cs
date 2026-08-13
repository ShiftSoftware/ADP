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
        IEnumerable<ServiceItemEligibilityConditionModel> conditions,
        long? baseScheduleMaximumMileage = null)
    {
        foreach (var condition in conditions ?? Enumerable.Empty<ServiceItemEligibilityConditionModel>())
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

    private bool MatchesServiceHistoryCondition(ServiceItemEligibilityConditionModel condition)
    {
        var requiredValues = condition.Values?.ToList();
        if (condition.Operator != ServiceItemEligibilityConditionOperator.ContainsAll ||
            (condition.ValueMatch != ServiceItemEligibilityConditionValueMatch.Exact &&
                condition.ValueMatch != ServiceItemEligibilityConditionValueMatch.EndsWith) ||
            condition.Scope?.Selection != ServiceItemEligibilityConditionSelection.Latest ||
            condition.Scope.Count is null ||
            condition.Scope.Count <= 0 ||
            requiredValues is null ||
            requiredValues.Count == 0 ||
            requiredValues.Any(string.IsNullOrWhiteSpace))
            return false;

        var latestInvoices = VehicleServiceHistoryEvaluator.GetInvoices(companyDataAggregate, ConsistencyLevels.Strong)
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
            .Take(condition.Scope.Count.Value)
            .ToList();

        if (latestInvoices.Count != condition.Scope.Count.Value)
            return false;

        var packageCodes = latestInvoices
            .SelectMany(x => x.Invoice.LaborLines ?? Enumerable.Empty<OrderLaborLineModel>())
            .Select(line => line.PackageCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList();

        return requiredValues.All(value =>
            packageCodes.Any(code => MatchesValue(code, value, condition.ValueMatch)));
    }

    private static bool MatchesBaseScheduleMaximumMileageCondition(
        ServiceItemEligibilityConditionModel condition,
        long? baseScheduleMaximumMileage)
    {
        var values = condition.Values?.ToList();
        return condition.Operator == ServiceItemEligibilityConditionOperator.Equals &&
            condition.ValueMatch == ServiceItemEligibilityConditionValueMatch.Exact &&
            condition.Scope is null &&
            values is { Count: 1 } &&
            long.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var requiredMileage) &&
            requiredMileage > 0 &&
            baseScheduleMaximumMileage == requiredMileage;
    }

    private static bool MatchesValue(
        string actual,
        string required,
        ServiceItemEligibilityConditionValueMatch valueMatch) =>
        valueMatch switch
        {
            ServiceItemEligibilityConditionValueMatch.Exact =>
                string.Equals(actual, required, StringComparison.OrdinalIgnoreCase),
            ServiceItemEligibilityConditionValueMatch.EndsWith =>
                actual.EndsWith(required, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
}
