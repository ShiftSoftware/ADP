using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

/// <summary>
/// The DMS report's derived margin / cost / profit arithmetic over a generated <see cref="MenuLineDTO"/>.
///
/// These used to be computed properties ON the DTO. They live here because the shared
/// <see cref="Generation.MenuCodeGenerator"/> deliberately does not carry margin arithmetic — it is
/// presentation, and a pure function of fields the line already has (COSMOS_REPLICATION_PLAN.md §14).
/// With layer 1 refusing it, leaving it on layer 3 gave the same arithmetic two plausible homes; this
/// gives it one, in the assembly that owns <see cref="IMenuReportExporter"/>, while
/// <see cref="MenuLineDTO"/> stays plain data in the DTO package.
///
/// To be precise about the dealer-cost angle, because it is easy to overstate: <see cref="MenuLineDTO"/>
/// is EXPORT-ONLY and was never itself a leak path. The concern is forward-looking — when the vehicle
/// lookup gets its own line DTO (Phase 6), this type is the obvious template, and <c>PartsCost</c> /
/// <c>PartsProfit</c> / <c>GrossProfit</c> cannot be copied across without dragging dealer cost with
/// them. Cost is kept out of the lookup at its real chokepoint, <c>MenuGenerationConfig.IncludePartCost</c>.
///
/// Every formula below is the previous DTO implementation VERBATIM, including its rounding and its
/// divide-by-zero guards. The Phase 0 golden snapshots render all of them, so the move is provably
/// output-preserving — do not "tidy" a formula here without re-reading those snapshots.
///
/// They are extension members, so existing report code that reads <c>line.MenuTotalPrice</c> (or
/// <c>oldLine?.MenuTotalPrice</c>) keeps compiling as long as this namespace is imported — any
/// implementer of <see cref="IMenuReportExporter"/> already imports it.
///
/// ONE CAVEAT: extension members are invisible to serializers and to reflection-driven mapping (JSON,
/// reflection-based Excel writers). Nothing serializes <see cref="MenuLineDTO"/> — every
/// export endpoint returns a byte array — but a consumer that starts doing so gets a payload without
/// these figures, silently. Project them explicitly if that day comes.
/// </summary>
public static class MenuLineMargins
{
    extension(MenuLineDTO line)
    {
        public decimal PartsCost => line.Parts?.Sum(x => x.TotalCost) ?? 0;

        public decimal PartsPrice => line.Parts?.Sum(x => x.TotalPrice) ?? 0;

        public decimal PartsProfit => line.PartsPrice - line.PartsCost;

        public decimal PartsProfitPercentage
        {
            get
            {
                if (line.PartsCost == 0)
                    return 0;

                return Math.Round((line.PartsProfit / line.PartsCost) * 100, 2);
            }
        }

        /// <summary>Internal labour cost — a flat 10 per allowed hour, not the dealer's labour rate.</summary>
        public decimal LabourCost => 10 * line.AllowedTime;

        public decimal LabourPrice => line.LabourRate * line.AllowedTime;

        public decimal LabourTotalPrice => line.LabourPrice + line.Consumable;

        public decimal LabourProfit => line.LabourPrice - line.LabourCost;

        public decimal GrossProfit => line.PartsProfit + line.LabourProfit;

        public decimal GrossProfitPercentage
        {
            get
            {
                decimal revenue = line.PartsPrice + line.LabourPrice;
                if (revenue == 0)
                    return 0;

                return Math.Round((line.GrossProfit / revenue) * 100, 2);
            }
        }

        public decimal MenuProfit => line.PartsProfit + line.LabourProfit + line.Consumable;

        /// <summary>Labour total plus parts, less the variant's discount percentage.</summary>
        public decimal MenuTotalPrice
        {
            get
            {
                decimal totalPrice = line.LabourTotalPrice + line.PartsPrice;

                // Calculate discount
                totalPrice = totalPrice - (line.DiscountPercentage.GetValueOrDefault() / 100 * totalPrice);

                return totalPrice;
            }
        }
    }
}
