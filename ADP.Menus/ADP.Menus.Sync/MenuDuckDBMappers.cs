using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.DuckDB;

namespace ShiftSoftware.ADP.Menus.Sync;

/// <summary>
/// Manual EF entity → DuckDB row projections, the normalized counterpart of
/// <see cref="Replication.MenuCosmosMappers"/> — and deliberately much smaller: every method is a
/// scalar copy of one row, because there is nothing to embed. Cosmos cannot join, so its projections
/// denormalize reference data into the menu documents; DuckDB joins, so each table carries only its
/// own fields plus the ids the reader joins on.
///
/// The rules that DO hold here are the same ones the Cosmos projections follow:
///  • the row id is the source row's database id;
///  • <c>IsDeleted</c> is carried, never filtered — whether a soft-deleted row contributes to a menu
///    is decided once, by the generation layer, so the DMS export and both lookups cannot disagree;
///  • <c>LastSaveDate</c> is carried as the sync's incremental watermark.
/// </summary>
public static class MenuDuckDBMappers
{
    public static MenuDuckDBModel Map(Menu menu) => new()
    {
        ID = menu.ID,
        BasicModelCode = menu.BasicModelCode,
        VehicleModelID = menu.VehicleModelID,
        IsDeleted = menu.IsDeleted,
        LastSaveDate = menu.LastSaveDate,
    };

    public static MenuVehicleModelDuckDBModel Map(VehicleModel vehicleModel) => new()
    {
        ID = vehicleModel.ID,
        Name = vehicleModel.Name,
        BrandID = vehicleModel.BrandID,
        IsDeleted = vehicleModel.IsDeleted,
        LastSaveDate = vehicleModel.LastSaveDate,
    };

    public static MenuVariantDuckDBModel Map(MenuVariant variant) => new()
    {
        ID = variant.ID,
        MenuID = variant.MenuID,
        Name = variant.Name,
        MenuPrefix = variant.MenuPrefix,
        MenuPostfix = variant.MenuPostfix,
        StandaloneMenuPrefix = variant.StandaloneMenuPrefix,
        StandaloneMenuPostfix = variant.StandaloneMenuPostfix,
        LabourRate = variant.LabourRate,
        DiscountPercentage = variant.DiscountPercentage,
        IsFree = variant.IsFree,
        HasStandaloneItems = variant.HasStandaloneItems,
        IsDeleted = variant.IsDeleted,
        LastSaveDate = variant.LastSaveDate,
    };

    public static MenuVariantLabourRateDuckDBModel Map(MenuVariantLabourRate rate) => new()
    {
        ID = rate.ID,
        MenuVariantID = rate.MenuVariantID,
        CountryID = rate.CountryID,
        LabourRate = rate.LabourRate,
        IsDeleted = rate.IsDeleted,
        LastSaveDate = rate.LastSaveDate,
    };

    public static MenuPeriodicAvailabilityDuckDBModel Map(MenuPeriodicAvailability period) => new()
    {
        ID = period.ID,
        MenuVariantID = period.MenuVariantID,
        ServiceIntervalID = period.ServiceIntervalID,
        IsDeleted = period.IsDeleted,
        LastSaveDate = period.LastSaveDate,
    };

    public static MenuLabourDetailsDuckDBModel Map(MenuLabourDetails labour) => new()
    {
        ID = labour.ID,
        MenuVariantID = labour.MenuVariantID,
        ServiceIntervalGroupID = labour.ServiceIntervalGroupID,
        AllowedTime = labour.AllowedTime,
        Consumable = labour.Consumable,
        IsDeleted = labour.IsDeleted,
        LastSaveDate = labour.LastSaveDate,
    };

    public static MenuItemDuckDBModel Map(MenuItem item) => new()
    {
        ID = item.ID,
        MenuVariantID = item.MenuVariantID,
        ReplacementItemVehicleModelID = item.ReplacementItemVehicleModelID,
        StandaloneAllowedTime = item.StandaloneAllowedTime,
        IsDeleted = item.IsDeleted,
        LastSaveDate = item.LastSaveDate,
    };

    public static MenuItemPartDuckDBModel Map(MenuItemPart part) => new()
    {
        ID = part.ID,
        MenuItemID = part.MenuItemID,
        SortOrder = part.SortOrder,
        PartNumber = part.PartNumber,
        PeriodicQuantity = part.PeriodicQuantity,
        StandaloneQuantity = part.StandaloneQuantity,
        IsDeleted = part.IsDeleted,
        LastSaveDate = part.LastSaveDate,
    };

    public static MenuItemPartCountryPriceDuckDBModel Map(MenuItemPartCountryPrice price) => new()
    {
        ID = price.ID,
        MenuItemPartID = price.MenuItemPartID,
        CountryID = price.CountryID,
        PartPrice = price.PartPrice,
        PartFinalPrice = price.PartFinalPrice,
        IsDeleted = price.IsDeleted,
        LastSaveDate = price.LastSaveDate,
    };

    public static ServiceIntervalDuckDBModel Map(ServiceInterval interval) => new()
    {
        ID = interval.ID,
        Code = interval.Code,
        Description = interval.Description,
        ValueInMeter = interval.ValueInMeter,
        ServiceIntervalGroupID = interval.ServiceIntervalGroupID,
        IsDeleted = interval.IsDeleted,
        LastSaveDate = interval.LastSaveDate,
    };

    public static ServiceIntervalGroupDuckDBModel Map(ServiceIntervalGroup group) => new()
    {
        ID = group.ID,
        LabourCode = group.LabourCode,
        IsDeleted = group.IsDeleted,
        LastSaveDate = group.LastSaveDate,
    };

    public static ReplacementItemDuckDBModel Map(ReplacementItem replacementItem) => new()
    {
        ID = replacementItem.ID,
        FriendlyName = replacementItem.FriendlyName,
        StandaloneOperationCode = replacementItem.StandaloneOperationCode,
        StandaloneLabourCode = replacementItem.StandaloneLabourCode,
        StandaloneReplacementItemGroupID = replacementItem.StandaloneReplacementItemGroupID,
        IsDeleted = replacementItem.IsDeleted,
        LastSaveDate = replacementItem.LastSaveDate,
    };

    public static ReplacementItemServiceIntervalGroupDuckDBModel Map(ReplacementItemServiceIntervalGroup link) => new()
    {
        ID = link.ID,
        ReplacementItemID = link.ReplacementItemID,
        ServiceIntervalGroupID = link.ServiceIntervalGroupID,
        IsDeleted = link.IsDeleted,
        LastSaveDate = link.LastSaveDate,
    };

    public static ReplacementItemVehicleModelDuckDBModel Map(ReplacementItemVehicleModel link) => new()
    {
        ID = link.ID,
        ReplacementItemID = link.ReplacementItemID,
        IsDeleted = link.IsDeleted,
        LastSaveDate = link.LastSaveDate,
    };

    public static StandaloneReplacementItemGroupDuckDBModel Map(StandaloneReplacementItemGroup group) => new()
    {
        ID = group.ID,
        Name = group.Name,
        MenuCode = group.MenuCode,
        LabourCode = group.LabourCode,
        IsDeleted = group.IsDeleted,
        LastSaveDate = group.LastSaveDate,
    };

    public static LabourRateMappingDuckDBModel Map(LabourRateMapping mapping) => new()
    {
        ID = mapping.ID,
        BrandID = mapping.BrandID,
        LabourRate = mapping.LabourRate,
        Code = mapping.Code,
        IsDeleted = mapping.IsDeleted,
        LastSaveDate = mapping.LastSaveDate,
    };

    public static BrandMappingDuckDBModel Map(BrandMapping mapping) => new()
    {
        ID = mapping.ID,
        BrandID = mapping.BrandID,
        Code = mapping.Code,
        BrandAbbreviation = mapping.BrandAbbreviation,
        IsDeleted = mapping.IsDeleted,
        LastSaveDate = mapping.LastSaveDate,
    };
}
