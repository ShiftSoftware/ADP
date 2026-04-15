using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

public class MenuExportService
{
    public static IEnumerable<MenuLineDTO> GenerateMenuLines(
            List<MenuVariant> menuVariants,
            Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> labourRateMapping,
            Dictionary<long?, BrandMapping> brandMapping,
            long countryId,
            decimal transferRate,
            string? language = null,
            bool usePrimaryLabourRate = false,
            bool applyPrefixPostfixToStandalones = false)
    {
        IEnumerable<MenuLineDTO> result = [];

        foreach (var menuVariant in menuVariants)
        {
            foreach (var serviceInterval in menuVariant.PeriodicAvailabilities)
            {
                var code = $"{menuVariant.MenuPrefix} {menuVariant.Menu.BasicModelCode} {serviceInterval.ServiceInterval.Code} {menuVariant.MenuPostfix}".Trim();

                string labourCode = "";
                decimal allowedTime = 0;
                decimal consumable = 0;

                var labourDetail = menuVariant.LabourDetails.FirstOrDefault(x => x.ServiceIntervalGroup.ServiceIntervals.Any(s => s.ID == serviceInterval.ServiceIntervalID));
                if (labourDetail is not null)
                {
                    consumable = Math.Round(labourDetail.Consumable * transferRate, 2);
                    allowedTime = labourDetail.AllowedTime;

                    var allowedTimeText = Utility.GetAllowedTimeText(labourDetail.AllowedTime);

                    var primaryLabourRate = menuVariant.LabourRate;
                    var labourRateMap = labourRateMapping[new CompositeKey<long?, decimal>(menuVariant.Menu.VehicleModel!.BrandID, primaryLabourRate)];
                    var brandAbbreviation = brandMapping.GetValueOrDefault(menuVariant.Menu.VehicleModel!.BrandID)?.BrandAbbreviation ?? "Z";

                    labourCode = $"{labourDetail.ServiceIntervalGroup.LabourCode}{allowedTimeText}{labourRateMap.Code}{brandAbbreviation}".Trim();
                }
                else
                {
                    continue;
                }

                var item = new MenuLineDTO
                {
                    Code = code,
                    BrandID = menuVariant.Menu.VehicleModel!.BrandID,
                    BasicModelCode = menuVariant.Menu.BasicModelCode,
                    Model = menuVariant.Menu.VehicleModel!.Name,
                    Description = serviceInterval.ServiceInterval.Description,
                    LabourCode = labourCode,
                    Consumable = consumable,
                    AllowedTime = allowedTime,
                    LabourRate = ResolveLabourRate(menuVariant, countryId, usePrimaryLabourRate),
                    DiscountPercentage = menuVariant.DiscountPercentage,
                    IsStandalone = false,
                    Parts = menuVariant.Items
                        .Where(x => x.ReplacementItemVehicleModel!.ReplacementItem.ReplacementItemServiceIntervalGroups
                            .Any(a => a.ServiceIntervalGroup.ServiceIntervals.Any(i => i.ID == serviceInterval.ServiceIntervalID)))
                        .SelectMany(x => x.Parts.Where(p => !p.IsDeleted && p.PeriodicQuantity.GetValueOrDefault() > 0).Select(p => new MenuLinePartDTO
                        {
                            PartNumber = p.PartNumber,
                            Quantity = p.PeriodicQuantity.GetValueOrDefault(),
                            Price = GetPartFinalPriceByCountry(p, countryId),
                            Cost = GetPartPriceByCountry(p, countryId),
                        }))
                };

                result = result.Append(item);
            }
        }

        result = result.Concat(GenerateStandaloneMenuLines(menuVariants, labourRateMapping, brandMapping, countryId, language, usePrimaryLabourRate, applyPrefixPostfixToStandalones));

        return result;
    }

    private static decimal ResolveLabourRate(MenuVariant menuVariant, long countryId, bool usePrimary)
    {
        if (usePrimary)
            return menuVariant.LabourRate;
        return menuVariant.LabourRates.FirstOrDefault(x => !x.IsDeleted && x.CountryID == countryId)?.LabourRate
            ?? menuVariant.LabourRate;
    }

    private static IEnumerable<MenuLineDTO> GenerateStandaloneMenuLines(
            List<MenuVariant> menuVariants,
            Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> labourRateMapping,
            Dictionary<long?, BrandMapping> brandMapping,
            long countryId,
            string? language = null,
            bool usePrimaryLabourRate = false,
            bool applyPrefixPostfixToStandalones = false)
    {
        IEnumerable<MenuLineDTO> result = [];

        // Get only menus that include standalone items
        var standaloneMenus = menuVariants.Where(x => x.HasStandaloneItems).ToList();

        foreach (var menuVariant in standaloneMenus)
        {
            // Create menu for non grouped items
            var nonGroupedItems = menuVariant.Items
                .Where(x => x.ReplacementItemVehicleModel!.ReplacementItem.StandaloneReplacementItemGroup is null &&
                    x.Parts.Any(p => !p.IsDeleted && p.StandaloneQuantity.GetValueOrDefault() > 0))
                .ToList();

            foreach (var item in nonGroupedItems)
            {
                var standaloneOperationCode = LocalizedText.Resolve(item.ReplacementItemVehicleModel!.ReplacementItem.StandaloneOperationCode, language);
                var code = applyPrefixPostfixToStandalones
                    ? $"{menuVariant.MenuPrefix} {standaloneOperationCode} {menuVariant.Menu.BasicModelCode} {menuVariant.MenuPostfix}".Trim()
                    : $"{standaloneOperationCode} {menuVariant.Menu.BasicModelCode}".Trim();

                decimal allowedTime = item.StandaloneAllowedTime;

                var allowedTimeText = Utility.GetAllowedTimeText(item.StandaloneAllowedTime);

                var primaryLabourRate = menuVariant.LabourRate;
                var labourRate = ResolveLabourRate(menuVariant, countryId, usePrimaryLabourRate);
                var labourRateMap = labourRateMapping[new CompositeKey<long?, decimal>(menuVariant.Menu.VehicleModel!.BrandID, primaryLabourRate)];
                var brandAbbreviation = brandMapping.GetValueOrDefault(menuVariant.Menu.VehicleModel!.BrandID)?.BrandAbbreviation ?? "Z";

                var labourCode = $"{item.ReplacementItemVehicleModel!.ReplacementItem.StandaloneLabourCode}{allowedTimeText}{labourRateMap.Code}{brandAbbreviation}".Trim();

                var line = new MenuLineDTO
                {
                    Code = code,
                    Model = menuVariant.Menu.VehicleModel!.Name,
                    AllowedTime = allowedTime,
                    Consumable = 0,
                    LabourRate = labourRate,
                    LabourCode = labourCode,
                    BasicModelCode = menuVariant.Menu.BasicModelCode,
                    BrandID = menuVariant.Menu.VehicleModel!.BrandID,
                    Description = item.ReplacementItemVehicleModel!.ReplacementItem.FriendlyName,
                    DiscountPercentage = menuVariant.DiscountPercentage,
                    IsStandalone = true,
                    Parts = item.Parts
                        .Where(x => !x.IsDeleted && x.StandaloneQuantity.GetValueOrDefault() > 0)
                        .Select(x => new MenuLinePartDTO
                        {
                            PartNumber = x.PartNumber,
                            Price = GetPartFinalPriceByCountry(x, countryId),
                            Quantity = x.StandaloneQuantity.GetValueOrDefault(),
                            Cost = GetPartPriceByCountry(x, countryId),
                        })
                };
                result = result.Append(line);
            }

            // Create menu for grouped items
            var groupedItems = menuVariant.Items.Where(x => x.ReplacementItemVehicleModel?.ReplacementItem?.StandaloneReplacementItemGroup is not null &&
                x.Parts.Any(p => !p.IsDeleted && p.StandaloneQuantity.GetValueOrDefault() > 0))
                .GroupBy(x => x.ReplacementItemVehicleModel!.ReplacementItem!.StandaloneReplacementItemGroup!.ID).ToList();

            foreach (var item in groupedItems)
            {
                var menuCode = LocalizedText.Resolve(item.First().ReplacementItemVehicleModel!.ReplacementItem!.StandaloneReplacementItemGroup!.MenuCode, language);
                var code = applyPrefixPostfixToStandalones
                    ? $"{menuVariant.MenuPrefix} {menuCode} {menuVariant.Menu.BasicModelCode} {menuVariant.MenuPostfix}".Trim()
                    : $"{menuCode} {menuVariant.Menu.BasicModelCode}".Trim();

                decimal allowedTime = item.First().StandaloneAllowedTime;

                var allowedTimeText = Utility.GetAllowedTimeText(allowedTime);

                var primaryLabourRate = menuVariant.LabourRate;
                var labourRate = ResolveLabourRate(menuVariant, countryId, usePrimaryLabourRate);
                var labourRateMap = labourRateMapping[new CompositeKey<long?, decimal>(menuVariant.Menu.VehicleModel!.BrandID, primaryLabourRate)];
                var brandAbbreviation = brandMapping.GetValueOrDefault(menuVariant.Menu.VehicleModel!.BrandID)?.BrandAbbreviation ?? "Z";

                var labourCode = $"{item.First().ReplacementItemVehicleModel!.ReplacementItem!.StandaloneReplacementItemGroup!.LabourCode}{allowedTimeText}{labourRateMap.Code}{brandAbbreviation}".Trim();

                var line = new MenuLineDTO
                {
                    Code = code,
                    Model = menuVariant.Menu.VehicleModel!.Name,
                    AllowedTime = allowedTime,
                    Consumable = 0,
                    LabourRate = labourRate,
                    LabourCode = labourCode,
                    BasicModelCode = menuVariant.Menu.BasicModelCode,
                    BrandID = menuVariant.Menu.VehicleModel!.BrandID,
                    Description = item.First().ReplacementItemVehicleModel!.ReplacementItem!.StandaloneReplacementItemGroup!.Name,
                    DiscountPercentage = menuVariant.DiscountPercentage,
                    IsStandalone = true,
                    Parts = item.SelectMany(x => x.Parts.Where(p => !p.IsDeleted && p.StandaloneQuantity.GetValueOrDefault() > 0).Select(p => new MenuLinePartDTO
                    {
                        PartNumber = p.PartNumber,
                        Quantity = p.StandaloneQuantity.GetValueOrDefault(),
                        Price = GetPartFinalPriceByCountry(p, countryId),
                        Cost = GetPartPriceByCountry(p, countryId),
                    }))
                };
                result = result.Append(line);
            }
        }

        return result;
    }

    internal static decimal GetPartPriceByCountry(MenuItemPart part, long countryId)
    {
        var row = part.CountryPrices?.FirstOrDefault(x => !x.IsDeleted && x.CountryID == countryId);
        return row?.PartPrice ?? 0;
    }

    internal static decimal GetPartFinalPriceByCountry(MenuItemPart part, long countryId)
    {
        var row = part.CountryPrices?.FirstOrDefault(x => !x.IsDeleted && x.CountryID == countryId);
        return row?.PartFinalPrice ?? 0;
    }
}
