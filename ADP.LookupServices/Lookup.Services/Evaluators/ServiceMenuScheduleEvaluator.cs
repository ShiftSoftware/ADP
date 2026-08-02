using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Menus.Generation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Shapes the generator's flat line stream into what a service advisor actually reads: per variant, the
/// scheduled services in mileage order, and the standalone services that can be sold on their own.
///
/// <para>The generator emits every periodic line for every variant first, then every standalone line —
/// an order that exists to match the DMS export's spreadsheet, not to be read by a human. Nothing is
/// dropped or merged here; the same lines come out, grouped and sorted.</para>
///
/// <para><b>Sorting is presentation only.</b> It never decides which line is generated, so it cannot
/// change a menu code — unlike the ordering inside
/// <see cref="CosmosToGenerationAggregator"/>, which does. Scheduled services sort by distance because
/// that is the axis a service schedule is read along; a line with no distance (which a periodic line
/// should always have) sorts last rather than first, so a data gap cannot masquerade as "due at 0 km".</para>
/// </summary>
public static class ServiceMenuScheduleEvaluator
{
    /// <summary>
    /// Groups priced lines under their variants. Variant metadata comes off the lines themselves — the
    /// generator stamps every line with its variant and brand, precisely so consumers need no second
    /// lookup. (It stamps the model name too; the lookup drops it — see
    /// <see cref="ServiceMenuVariantDTO"/>.)
    /// </summary>
    /// <param name="lines">The generated lines, in generator order.</param>
    public static List<ServiceMenuVariantDTO> Evaluate(IReadOnlyList<GeneratedMenuLine> lines)
    {
        if (lines is null || lines.Count == 0)
            return new List<ServiceMenuVariantDTO>();

        var variants = new List<ServiceMenuVariantDTO>();

        // GroupBy preserves first-appearance order, and the aggregator already ordered variants by id,
        // so the result follows the same order the request was built in.
        foreach (var group in lines.GroupBy(line => line.VariantID))
        {
            var first = group.First();

            var periodic = group
                .Where(line => !line.IsStandalone)
                .OrderBy(line => line.ServiceIntervalValueInMeter ?? int.MaxValue)
                .ThenBy(line => line.ServiceIntervalCode, StringComparer.Ordinal)
                .Select(ServiceMenuPricingEvaluator.Evaluate)
                .ToList();

            // Ungrouped items before groups, mirroring the generator's own emission order; then by the
            // code so a UI list is stable across languages that reorder nothing else.
            var standalone = group
                .Where(line => line.IsStandalone)
                .OrderBy(line => line.LineType == MenuLineType.StandaloneGrouped ? 1 : 0)
                .ThenBy(line => line.Code, StringComparer.Ordinal)
                .Select(ServiceMenuPricingEvaluator.Evaluate)
                .ToList();

            variants.Add(new ServiceMenuVariantDTO
            {
                VariantID = first.VariantID,
                VariantName = first.VariantName,
                BrandID = first.BrandID?.ToString(),
                BrandCode = first.BrandCode,
                DiscountPercentage = first.DiscountPercentage,
                PeriodicServices = periodic,
                StandaloneServices = standalone,
            });
        }

        return variants;
    }
}
