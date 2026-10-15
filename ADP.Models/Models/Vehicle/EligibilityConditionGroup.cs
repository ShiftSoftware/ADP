using System;
using System.Globalization;
using System.Linq;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Closed shape of the condition grammar, shared by authoring and evaluation: the OR group and the
/// leaf shapes the lookup evaluator supports. Authoring applies the same leaf rules inside and outside
/// a group, so a mistake is rejected wherever it is written. Existing leaf payloads are unchanged.
/// </summary>
public static class EligibilityConditionGroup
{
    public static bool IsValid(EligibilityConditionModel group) =>
        group is not null && group.AnyOf is not null && group.AnyOf.Any()
        && group.UnsupportedProperties?.Count is not > 0
        && group.Field is null && group.Values is null && group.Scope is null
        && group.Program is null && group.Qualifier is null
        && group.Operator == EligibilityConditionOperator.ContainsAll
        && group.ValueMatch == EligibilityConditionValueMatch.Exact
        && Enum.IsDefined(typeof(EligibilityConditionUnmetBehavior), group.WhenUnmet)
        && group.AnyOf.All(IsSupportedAlternative);

    /// <summary>An ordinary condition: a supported field in a shape the evaluator reads, with a defined unmet behavior.</summary>
    public static bool IsSupportedLeaf(EligibilityConditionModel leaf) =>
        leaf is not null && leaf.AnyOf is null
        && Enum.IsDefined(typeof(EligibilityConditionUnmetBehavior), leaf.WhenUnmet)
        && HasSupportedShape(leaf);

    // Alternatives are predicates; only the group owns the failure behavior.
    private static bool IsSupportedAlternative(EligibilityConditionModel leaf) =>
        IsSupportedLeaf(leaf) && leaf.WhenUnmet == EligibilityConditionUnmetBehavior.Hide;

    private static bool HasSupportedShape(EligibilityConditionModel leaf)
    {
        if (leaf.UnsupportedProperties?.Count > 0 || leaf.Scope?.UnsupportedProperties?.Count > 0
            || leaf.Qualifier?.UnsupportedProperties?.Count > 0)
            return false;
        var values = leaf.Values?.ToList();
        if (leaf.Scope?.InspectionTypeID is not null && leaf.Field != "vehicleInspections.companyID") return false;
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace)) return false;
        bool plain = leaf.ValueMatch == EligibilityConditionValueMatch.Exact
            && leaf.Scope is null && leaf.Program is null && leaf.Qualifier is null;
        switch (leaf.Field)
        {
            case "isAuthorized":
                return plain && leaf.Operator == EligibilityConditionOperator.Equals
                    && values.Count == 1 && bool.TryParse(values[0], out _);
            case "vehicleInspections.companyID":
                return leaf.Operator == EligibilityConditionOperator.ContainsAll && leaf.ValueMatch == EligibilityConditionValueMatch.Exact
                    && leaf.Program is null && leaf.Qualifier is null && values.All(PositiveInteger)
                    && leaf.Scope?.Selection == EligibilityConditionSelection.All && leaf.Scope.Count is null && leaf.Scope.InspectionTypeID > 0;
            case "vehicleClassifications.code":
                return plain && leaf.Operator == EligibilityConditionOperator.ContainsAll;
            case "campaignVinEntries.campaignID":
                return plain && leaf.Operator == EligibilityConditionOperator.ContainsAll && values.All(PositiveInteger);
            case "serviceItems.baseSchedule.maximumMileage":
                return plain && leaf.Operator == EligibilityConditionOperator.Equals && values.Count == 1 && PositiveInteger(values[0]);
            case "serviceHistory.laborLines.maximumMilestone":
                return leaf.Operator == EligibilityConditionOperator.Equals && leaf.ValueMatch == EligibilityConditionValueMatch.Exact
                    && leaf.Scope is null && values.Count == 1 && PositiveInteger(values[0]) && MilestoneFilters(leaf);
            case "serviceHistory.laborLines.packageCode":
                if (leaf.Operator != EligibilityConditionOperator.ContainsAll || leaf.Scope is null) return false;
                bool scope = leaf.Scope.Selection == EligibilityConditionSelection.All ? leaf.Scope.Count is null
                    : leaf.Scope.Selection == EligibilityConditionSelection.Latest && leaf.Scope.Count > 0;
                return scope && (leaf.ValueMatch == EligibilityConditionValueMatch.Milestone
                    ? values.All(PositiveInteger) && MilestoneFilters(leaf)
                    : (leaf.ValueMatch == EligibilityConditionValueMatch.Exact || leaf.ValueMatch == EligibilityConditionValueMatch.EndsWith)
                        && leaf.Program is null && leaf.Qualifier is null);
            default: return false;
        }
    }

    private static bool PositiveInteger(string value) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0;

    private static bool MilestoneFilters(EligibilityConditionModel leaf)
    {
        if (leaf.Program is not null && (!leaf.Program.Any() || leaf.Program.Any(string.IsNullOrWhiteSpace))) return false;
        var qualifier = leaf.Qualifier;
        if (qualifier is null) return false;
        switch (qualifier.Selection)
        {
            case EligibilityConditionQualifierSelection.None:
            case EligibilityConditionQualifierSelection.Any:
                return qualifier.Values is null;
            case EligibilityConditionQualifierSelection.Only:
            case EligibilityConditionQualifierSelection.Except:
                return qualifier.Values is not null && qualifier.Values.Any() && !qualifier.Values.Any(string.IsNullOrWhiteSpace);
            default: return false;
        }
    }
}
