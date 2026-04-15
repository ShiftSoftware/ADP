using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Extensions;
using ShiftSoftware.ShiftEntity.EFCore;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data;

public class MenuDB : ShiftDbContext, IMenuDbContext
{
    public MenuDB(DbContextOptions option) : base(option)
    {
    }

    public DbSet<ReplacementItem> ReplacementItems { get; set; }
    public DbSet<VehicleModel> VehicleModels { get; set; }
    public DbSet<VehicleModelLabourRate> VehicleModelLabourRates { get; set; }
    public DbSet<ReplacementItemVehicleModel> ReplacementItemVehicleModels { get; set; }
    public DbSet<ReplacementItemVehicleModelPart> ReplacementItemVehicleModelParts { get; set; }
    public DbSet<MenuEntity> Menus { get; set; }
    public DbSet<MenuVariant> MenuVariants { get; set; }
    public DbSet<MenuVariantLabourRate> MenuVariantLabourRates { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<MenuItemPart> MenuItemParts { get; set; }
    public DbSet<MenuItemPartCountryPrice> MenuItemPartCountryPrices { get; set; }
    public DbSet<ServiceInterval> ServiceIntervals { get; set; }
    public DbSet<ServiceIntervalGroup> ServiceIntervalGroups { get; set; }
    public DbSet<LabourRateMapping> LabourRateMappings { get; set; }
    public DbSet<BrandMapping> BrandMappings { get; set; }
    public DbSet<MenuVersion> MenuVersions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureMenuEntities();
    }
}
