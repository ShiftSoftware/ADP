using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

/// <summary>
/// The DMS export's entry point into menu-code generation.
///
/// It no longer generates anything itself. It aggregates the EF graph into the source-agnostic
/// <see cref="MenuGenerationRequest"/>, calls the shared <see cref="MenuCodeGenerator"/> — the very
/// same service the vehicle lookup calls over Cosmos data — and maps the generic result onto the
/// export's report rows. That is what makes "the lookup's menu codes are identical to the export's" a
/// property of the build rather than a promise someone has to keep. See COSMOS_REPLICATION_PLAN.md §1.1.
///
/// Generation rules therefore belong in <see cref="MenuCodeGenerator"/> and nowhere else; the derived
/// margin / profit arithmetic belongs to the report layer (<see cref="MenuLineMargins"/>). This class
/// is only the adapter between them.
///
/// Behaviour is pinned by the Phase 0 golden snapshots, which run through this method: if they are
/// green, the export's output is byte-identical to what it produced before the refactor.
/// </summary>
public class MenuExportService
{
    /// <summary>
    /// Generates the export's report rows for one country / language configuration.
    ///
    /// Ordering is unchanged: all periodic lines (variant order, then interval order), then all
    /// standalone lines (ungrouped before grouped, per variant).
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// A variant's (brand, primary labour rate) pair has no <see cref="LabourRateMapping"/>. Callers
    /// pre-check the catalogue and fail up front with a friendlier message; this is the backstop, and
    /// it is deliberately preserved (open item O1).
    /// </exception>
    public static IEnumerable<MenuLineDTO> GenerateMenuLines(
            List<MenuVariant> menuVariants,
            Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> labourRateMapping,
            Dictionary<long?, BrandMapping> brandMapping,
            long countryId,
            decimal transferRate,
            string? language = null,
            bool usePrimaryLabourRate = false)
    {
        var request = EfToGenerationAggregator.Build(menuVariants, labourRateMapping, brandMapping);

        var config = new MenuGenerationConfig
        {
            CountryID = countryId,
            TransferRate = transferRate,
            Language = language,
            UsePrimaryLabourRate = usePrimaryLabourRate,

            // The DMS export is the only consumer allowed to see dealer cost, and it needs it for the
            // detail report's margin columns. The vehicle lookup leaves this off.
            IncludePartCost = true,
        };

        // Materialised rather than returned lazily: report exporters walk the sequence more than once
        // (once to size the parts columns, once to write the rows), and the fold is not cheap.
        return MenuCodeGenerator.Generate(request, config).Select(MapToReportLine).ToList();
    }

    private static MenuLineDTO MapToReportLine(GeneratedMenuLine line) => new()
    {
        Code = line.Code,
        BrandID = line.BrandID,
        BasicModelCode = line.BasicModelCode,
        Model = line.Model,
        Description = line.Description,
        LabourCode = line.LabourCode,
        LabourRate = line.LabourRate,
        AllowedTime = line.AllowedTime,
        Consumable = line.Consumable,
        DiscountPercentage = line.DiscountPercentage,
        IsStandalone = line.IsStandalone,
        Parts = line.Parts.Select(part => new MenuLinePartDTO
        {
            PartNumber = part.PartNumber,
            Quantity = part.Quantity,

            // Never null here — IncludePartCost is set above. An unpriced part already resolves to 0
            // inside the generator, which is the export's historical fallback.
            Cost = part.Cost.GetValueOrDefault(),
            Price = part.Price,
        }).ToList(),
    };
}
