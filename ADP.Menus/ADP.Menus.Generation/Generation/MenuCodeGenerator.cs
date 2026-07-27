using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// Generates service-menu lines — menu codes, labour codes and prices — from a source-agnostic
/// <see cref="MenuGenerationRequest"/>.
///
/// THIS IS THE SINGLE SOURCE OF TRUTH for menu-code generation. The DMS export and the vehicle lookup
/// both call it over the same contract, which is what makes "the lookup's codes are identical to the
/// export's" structural rather than a convention someone has to maintain.
///
/// It is a faithful port of <c>ADP.Menus.Data.DataServices.MenuExportService.GenerateMenuLines</c>:
/// EF navigations became dictionary lookups, and nothing else changed. Behaviour is locked by the
/// Phase 0 golden tests plus a differential test that runs both implementations over equivalent input
/// and compares them field by field. Any behaviour change here is a change to codes a DMS has already
/// received — treat it as a breaking change.
///
/// Pure and deterministic: no ambient state, no clock, no I/O. Output order follows input order
/// (all periodic lines for all variants, then all standalone lines), so a caller wanting stable
/// output supplies stable input.
/// </summary>
public static class MenuCodeGenerator
{
    /// <summary>
    /// Generates every menu line for the request under one language/country configuration.
    ///
    /// Ordering matches the source: all PERIODIC lines (in variant order, then interval order),
    /// followed by all STANDALONE lines (ungrouped before grouped, per variant).
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// No labour-rate mapping exists for a variant's (brand, primary labour rate) pair, or a referenced
    /// service interval / interval group is missing from <see cref="MenuGenerationReferenceData"/>. The source
    /// fails on these too (the export pre-checks the catalogue and refuses to run); preserved so bad
    /// reference data fails loudly instead of silently producing wrong codes.
    /// </exception>
    public static IEnumerable<GeneratedMenuLine> Generate(MenuGenerationRequest request, MenuGenerationConfig config)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (config is null) throw new ArgumentNullException(nameof(config));

        // Materialised rather than lazily concatenated so the caller cannot observe a half-built
        // sequence, and so ordering is explicit: all periodic first, then all standalone.
        var lines = new List<GeneratedMenuLine>();

        foreach (var variant in request.Variants)
            lines.AddRange(GeneratePeriodicLines(variant, request.Reference, config));

        foreach (var variant in request.Variants)
            lines.AddRange(GenerateStandaloneLines(variant, request.Reference, config));

        return lines;
    }

    // -------------------------------------------------------------------------------------------
    // Periodic lines — one per service interval the variant is available for, provided the variant
    // has labour details for a group containing that interval.
    // -------------------------------------------------------------------------------------------

    private static IEnumerable<GeneratedMenuLine> GeneratePeriodicLines(
        MenuGenerationVariant variant, MenuGenerationReferenceData reference, MenuGenerationConfig config)
    {
        var menuPrefix = MenuTextHelpers.Resolve(variant.MenuPrefix, config.Language);
        var menuPostfix = MenuTextHelpers.Resolve(variant.MenuPostfix, config.Language);

        foreach (var period in variant.Periods)
        {
            var interval = reference.Intervals[period.ServiceIntervalID];

            var code = $"{menuPrefix} {variant.BasicModelCode} {interval.Code} {menuPostfix}".Trim();

            // First labour detail whose interval group contains this interval. No match → the interval
            // produces NO line at all (the source `continue`s). A caller that loses this silently is
            // usually missing reference data, not authoring data.
            var labour = variant.Labours.FirstOrDefault(x =>
                reference.Groups[x.ServiceIntervalGroupID].ServiceIntervalIDs.Contains(period.ServiceIntervalID));

            if (labour is null)
                continue;

            var consumable = Math.Round(labour.Consumable * config.TransferRate, 2);
            var allowedTime = labour.AllowedTime;
            var allowedTimeText = MenuTextHelpers.GetAllowedTimeText(labour.AllowedTime);

            var labourRateCode = ResolveLabourRateCode(variant, reference);
            var brandMapping = ResolveBrandMapping(variant, reference);
            var brandAbbreviation = ResolveBrandAbbreviation(brandMapping);
            var groupLabourCode = reference.Groups[labour.ServiceIntervalGroupID].LabourCode;

            var labourCode = $"{groupLabourCode}{allowedTimeText}{labourRateCode}{brandAbbreviation}".Trim();

            yield return new GeneratedMenuLine
            {
                LineKey = PeriodicKey(variant.VariantID, period.ServiceIntervalID),
                LineType = MenuLineType.Periodic,
                Code = code,
                LabourCode = labourCode,
                // Verbatim, NOT language-resolved — matches the source.
                Description = interval.Description,

                MenuCodePrefix = menuPrefix,
                MenuCodeSegment = interval.Code,
                MenuCodePostfix = menuPostfix,

                LabourOperationCode = groupLabourCode,
                AllowedTimeText = allowedTimeText,
                LabourRateCode = labourRateCode,
                BrandAbbreviation = brandAbbreviation,

                ServiceIntervalID = period.ServiceIntervalID,
                ServiceIntervalGroupID = labour.ServiceIntervalGroupID,
                ServiceIntervalCode = interval.Code,
                ServiceIntervalValueInMeter = interval.ValueInMeter,

                BasicModelCode = variant.BasicModelCode,
                BrandID = variant.BrandID,
                BrandCode = brandMapping?.Code,
                Model = variant.Model,
                VariantID = variant.VariantID,
                VariantName = variant.VariantName,

                LabourRate = ResolveLabourRate(variant, config),
                PrimaryLabourRate = variant.LabourRate,
                AllowedTime = allowedTime,
                Consumable = consumable,
                RawConsumable = labour.Consumable,
                DiscountPercentage = variant.DiscountPercentage,

                CountryID = config.CountryID,
                TransferRate = config.TransferRate,
                Language = config.Language,

                Parts = variant.Items
                    .Where(IsLive)
                    .Where(item => item.ReplacementItemServiceIntervalGroupIDs
                        .Any(groupId => reference.Groups[groupId].ServiceIntervalIDs.Contains(period.ServiceIntervalID)))
                    .SelectMany(item => item.Parts
                        .Where(part => !part.IsDeleted && part.PeriodicQuantity.GetValueOrDefault() > 0)
                        .Select(part => MapPart(part, part.PeriodicQuantity.GetValueOrDefault(), config, item.MenuItemID)))
                    .ToList(),
            };
        }
    }

    // -------------------------------------------------------------------------------------------
    // Standalone lines — ungrouped (one per item) then grouped (one per standalone group).
    // -------------------------------------------------------------------------------------------

    private static IEnumerable<GeneratedMenuLine> GenerateStandaloneLines(
        MenuGenerationVariant variant, MenuGenerationReferenceData reference, MenuGenerationConfig config)
    {
        if (!variant.HasStandaloneItems)
            yield break;

        var standalonePrefix = MenuTextHelpers.Resolve(variant.StandaloneMenuPrefix, config.Language);
        var standalonePostfix = MenuTextHelpers.Resolve(variant.StandaloneMenuPostfix, config.Language);

        // Resolved INSIDE the loops, never before them: the labour-rate lookup throws when the mapping
        // is missing, and the source only performs it once it has a line to emit. Hoisting it would
        // make a variant that produces no standalone lines throw where the source does not.
        var standaloneItems = variant.Items
            .Where(IsLive)
            .Where(item => item.Parts.Any(part => !part.IsDeleted && part.StandaloneQuantity.GetValueOrDefault() > 0))
            .ToList();

        // --- ungrouped -------------------------------------------------------------------------
        foreach (var item in standaloneItems.Where(item => item.StandaloneGroup is null))
        {
            var operationCode = MenuTextHelpers.Resolve(item.StandaloneOperationCode, config.Language);
            var code = $"{standalonePrefix} {operationCode} {variant.BasicModelCode} {standalonePostfix}".Trim();
            var allowedTimeText = MenuTextHelpers.GetAllowedTimeText(item.StandaloneAllowedTime);
            var labourRateCode = ResolveLabourRateCode(variant, reference);
            var brandMapping = ResolveBrandMapping(variant, reference);
            var brandAbbreviation = ResolveBrandAbbreviation(brandMapping);

            yield return new GeneratedMenuLine
            {
                LineKey = StandaloneKey(variant.VariantID, item.MenuItemID),
                LineType = MenuLineType.StandaloneUngrouped,
                Code = code,
                LabourCode = $"{item.StandaloneLabourCode}{allowedTimeText}{labourRateCode}{brandAbbreviation}".Trim(),
                Description = item.FriendlyName,

                MenuCodePrefix = standalonePrefix,
                MenuCodeSegment = operationCode,
                MenuCodePostfix = standalonePostfix,

                LabourOperationCode = item.StandaloneLabourCode,
                AllowedTimeText = allowedTimeText,
                LabourRateCode = labourRateCode,
                BrandAbbreviation = brandAbbreviation,

                MenuItemID = item.MenuItemID,

                BasicModelCode = variant.BasicModelCode,
                BrandID = variant.BrandID,
                BrandCode = brandMapping?.Code,
                Model = variant.Model,
                VariantID = variant.VariantID,
                VariantName = variant.VariantName,

                LabourRate = ResolveLabourRate(variant, config),
                PrimaryLabourRate = variant.LabourRate,
                AllowedTime = item.StandaloneAllowedTime,
                Consumable = 0,
                RawConsumable = 0,
                DiscountPercentage = variant.DiscountPercentage,

                CountryID = config.CountryID,
                TransferRate = config.TransferRate,
                Language = config.Language,

                Parts = StandaloneParts(item, config).ToList(),
            };
        }

        // --- grouped ---------------------------------------------------------------------------
        var groups = standaloneItems
            .Where(item => item.StandaloneGroup is not null)
            .GroupBy(item => item.StandaloneGroup.ID);

        foreach (var group in groups)
        {
            // Allowed time comes from the FIRST item in the group — a source behaviour, preserved.
            // It follows input order, so the aggregator's ordering decides which item wins.
            var first = group.First();
            var standaloneGroup = first.StandaloneGroup;

            var menuCode = MenuTextHelpers.Resolve(standaloneGroup.MenuCode, config.Language);
            var code = $"{standalonePrefix} {menuCode} {variant.BasicModelCode} {standalonePostfix}".Trim();
            var allowedTime = first.StandaloneAllowedTime;
            var allowedTimeText = MenuTextHelpers.GetAllowedTimeText(allowedTime);
            var labourRateCode = ResolveLabourRateCode(variant, reference);
            var brandMapping = ResolveBrandMapping(variant, reference);
            var brandAbbreviation = ResolveBrandAbbreviation(brandMapping);

            yield return new GeneratedMenuLine
            {
                LineKey = StandaloneGroupKey(variant.VariantID, standaloneGroup.ID),
                LineType = MenuLineType.StandaloneGrouped,
                Code = code,
                LabourCode = $"{standaloneGroup.LabourCode}{allowedTimeText}{labourRateCode}{brandAbbreviation}".Trim(),
                Description = standaloneGroup.Name,

                MenuCodePrefix = standalonePrefix,
                MenuCodeSegment = menuCode,
                MenuCodePostfix = standalonePostfix,

                LabourOperationCode = standaloneGroup.LabourCode,
                AllowedTimeText = allowedTimeText,
                LabourRateCode = labourRateCode,
                BrandAbbreviation = brandAbbreviation,

                StandaloneGroupID = standaloneGroup.ID,

                BasicModelCode = variant.BasicModelCode,
                BrandID = variant.BrandID,
                BrandCode = brandMapping?.Code,
                Model = variant.Model,
                VariantID = variant.VariantID,
                VariantName = variant.VariantName,

                LabourRate = ResolveLabourRate(variant, config),
                PrimaryLabourRate = variant.LabourRate,
                AllowedTime = allowedTime,
                Consumable = 0,
                RawConsumable = 0,
                DiscountPercentage = variant.DiscountPercentage,

                CountryID = config.CountryID,
                TransferRate = config.TransferRate,
                Language = config.Language,

                Parts = group.SelectMany(item => StandaloneParts(item, config)).ToList(),
            };
        }
    }

    // -------------------------------------------------------------------------------------------
    // Shared resolution
    // -------------------------------------------------------------------------------------------

    /// <summary>An item participates only when it is live and its replacement-item link is live.</summary>
    private static bool IsLive(MenuGenerationItem item) =>
        !item.IsDeleted && item.HasReplacementItem && !item.ReplacementItemDeleted;

    private static IEnumerable<GeneratedMenuPart> StandaloneParts(MenuGenerationItem item, MenuGenerationConfig config) =>
        item.Parts
            .Where(part => !part.IsDeleted && part.StandaloneQuantity.GetValueOrDefault() > 0)
            .Select(part => MapPart(part, part.StandaloneQuantity.GetValueOrDefault(), config, item.MenuItemID));

    private static GeneratedMenuPart MapPart(MenuGenerationPart part, decimal quantity, MenuGenerationConfig config, long menuItemId)
    {
        var price = part.CountryPrices?.FirstOrDefault(x => !x.IsDeleted && x.CountryID == config.CountryID);

        return new GeneratedMenuPart
        {
            PartNumber = part.PartNumber,
            MenuItemID = menuItemId,
            SortOrder = part.SortOrder,
            Quantity = quantity,

            // Opt-in only. When requested, an unpriced part falls back to 0 exactly as the export does;
            // when not requested the property stays null so cost is absent rather than zeroed.
            Cost = config.IncludePartCost ? price?.PartPrice ?? 0 : (decimal?)null,

            Price = price?.PartFinalPrice ?? 0,
            HasCountryPrice = price is not null,
        };
    }

    /// <summary>
    /// The rate placed ON the line. Note this is NOT the labour-code lookup key — that is always the
    /// primary rate (see <see cref="ResolveLabourRateCode"/>).
    /// </summary>
    private static decimal ResolveLabourRate(MenuGenerationVariant variant, MenuGenerationConfig config)
    {
        if (config.UsePrimaryLabourRate)
            return variant.LabourRate;

        var countryRate = variant.CountryLabourRates
            .FirstOrDefault(x => !x.IsDeleted && x.CountryID == config.CountryID);

        return countryRate?.LabourRate ?? variant.LabourRate;
    }

    /// <summary>
    /// Keyed by the variant's PRIMARY labour rate, never the country-resolved one — so the labour code
    /// is stable across countries even when the rate on the line is not.
    /// </summary>
    private static string ResolveLabourRateCode(MenuGenerationVariant variant, MenuGenerationReferenceData reference) =>
        reference.LabourRateCodes[new MenuGenerationLabourRateKey(variant.BrandID, variant.LabourRate)];

    private static MenuGenerationBrandMapping ResolveBrandMapping(MenuGenerationVariant variant, MenuGenerationReferenceData reference) =>
        reference.BrandMappings.TryGetValue(variant.BrandID, out var mapping) ? mapping : null;

    /// <summary>Unmapped brands fall back to "Z" rather than failing — a source behaviour.</summary>
    private static string ResolveBrandAbbreviation(MenuGenerationBrandMapping mapping) =>
        mapping?.Abbreviation ?? "Z";

    internal static string PeriodicKey(long variantId, long serviceIntervalId) => $"P|{variantId}|{serviceIntervalId}";

    internal static string StandaloneKey(long variantId, long menuItemId) => $"S|{variantId}|{menuItemId}";

    internal static string StandaloneGroupKey(long variantId, long groupId) => $"G|{variantId}|{groupId}";
}
