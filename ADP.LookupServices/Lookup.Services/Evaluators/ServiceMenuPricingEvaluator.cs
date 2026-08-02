using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Menus.Generation;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// The money on a menu line: parts, labour, consumable, discount, total.
///
/// <para>This is the lookup's counterpart to the DMS export's <c>MenuLineMargins</c>, and the formulas
/// that overlap are that implementation VERBATIM — <c>LabourPrice = LabourRate × AllowedTime</c>,
/// <c>LabourTotalPrice = LabourPrice + Consumable</c>, and the total's discount arithmetic. They agree
/// because a customer quoted a price by a web component and the same menu priced in the DMS must not
/// differ by a rounding step.</para>
///
/// <para><b>Everything cost-derived is absent, and that is the point.</b> <c>PartsCost</c>,
/// <c>PartsProfit</c>, <c>GrossProfit</c>, <c>MenuProfit</c> and the two profit percentages exist on the
/// export's line and must never be copied here: they are dealer figures. There is nothing to strip in
/// practice — the lookup never asks the generator for cost, so <c>GeneratedMenuPart.Cost</c> is null all
/// the way down — but the omission is deliberate rather than accidental. Do not add them.</para>
///
/// <para>Pure and static: no clock, no I/O, no options. Everything it needs is on the generated line.</para>
/// </summary>
public static class ServiceMenuPricingEvaluator
{
    /// <summary>Prices one generated line into its DTO form.</summary>
    public static ServiceMenuLineDTO Evaluate(GeneratedMenuLine line)
    {
        var parts = (line.Parts ?? [])
            .Select(EvaluatePart)
            .ToList();

        var partsTotal = parts.Sum(part => part.TotalPrice);

        // Verbatim from the export: labour is rate × time, and the consumable rides on the labour side
        // (it is already transfer-rate scaled and rounded by the generator — never scale it again here).
        var labourPrice = line.LabourRate * line.AllowedTime;
        var labourTotal = labourPrice + line.Consumable;

        var beforeDiscount = labourTotal + partsTotal;

        // The export computes the discounted total in one expression rather than rounding a discount
        // amount first. Reproduced exactly, then the amount is derived BACK OUT of it — so the reported
        // amount always reconciles with the reported total, instead of being a second, separate
        // rounding that could disagree with it by a fraction.
        var total = beforeDiscount - (line.DiscountPercentage.GetValueOrDefault() / 100 * beforeDiscount);

        return new ServiceMenuLineDTO
        {
            LineKey = line.LineKey,
            Code = line.Code,
            LabourCode = line.LabourCode,
            Description = line.Description,
            LineType = MapLineType(line.LineType),
            IsStandalone = line.IsStandalone,

            ServiceIntervalCode = line.ServiceIntervalCode,
            ServiceIntervalValueInMeter = line.ServiceIntervalValueInMeter,

            LabourRate = line.LabourRate,
            AllowedTime = line.AllowedTime,
            LabourPrice = labourPrice,
            Consumable = line.Consumable,
            LabourTotalPrice = labourTotal,

            Parts = parts,
            PartsTotalPrice = partsTotal,

            DiscountPercentage = line.DiscountPercentage,
            DiscountAmount = beforeDiscount - total,
            TotalPrice = total,

            // A part with no price row for the country is priced 0 by fallback, which the generator
            // does not treat as an error — so the total is understated rather than wrong, and a caller
            // needs to be able to tell. Surfaced instead of hidden.
            HasUnpricedParts = parts.Any(part => !part.HasCountryPrice),
        };
    }

    /// <summary>Prices every line, preserving the generator's order.</summary>
    public static List<ServiceMenuLineDTO> Evaluate(IEnumerable<GeneratedMenuLine> lines) =>
        (lines ?? []).Select(Evaluate).ToList();

    private static ServiceMenuPartDTO EvaluatePart(GeneratedMenuPart part) => new ServiceMenuPartDTO
    {
        PartNumber = part.PartNumber,
        SortOrder = part.SortOrder,
        Quantity = part.Quantity,
        UnitPrice = part.Price,
        TotalPrice = part.TotalPrice,
        HasCountryPrice = part.HasCountryPrice,

        // No Cost / TotalCost. See the class remarks.
    };

    private static ServiceMenuLineType MapLineType(MenuLineType lineType) => lineType switch
    {
        MenuLineType.StandaloneUngrouped => ServiceMenuLineType.StandaloneUngrouped,
        MenuLineType.StandaloneGrouped => ServiceMenuLineType.StandaloneGrouped,
        _ => ServiceMenuLineType.Periodic,
    };
}
