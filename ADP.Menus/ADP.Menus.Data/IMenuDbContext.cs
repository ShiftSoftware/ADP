using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data;

public interface IMenuDbContext
{
    DbSet<ReplacementItem> ReplacementItems { get; set; }
    DbSet<VehicleModel> VehicleModels { get; set; }
    DbSet<VehicleModelLabourRate> VehicleModelLabourRates { get; set; }
    DbSet<ReplacementItemVehicleModel> ReplacementItemVehicleModels { get; set; }
    DbSet<ReplacementItemVehicleModelPart> ReplacementItemVehicleModelParts { get; set; }
    DbSet<MenuEntity> Menus { get; set; }
    DbSet<MenuVariant> MenuVariants { get; set; }
    DbSet<MenuVariantLabourRate> MenuVariantLabourRates { get; set; }
    DbSet<MenuItem> MenuItems { get; set; }
    DbSet<MenuItemPart> MenuItemParts { get; set; }
    DbSet<MenuItemPartCountryPrice> MenuItemPartCountryPrices { get; set; }
    DbSet<ServiceInterval> ServiceIntervals { get; set; }
    DbSet<ServiceIntervalGroup> ServiceIntervalGroups { get; set; }
    DbSet<LabourRateMapping> LabourRateMappings { get; set; }
    DbSet<BrandMapping> BrandMappings { get; set; }
    DbSet<MenuVersion> MenuVersions { get; set; }
}
