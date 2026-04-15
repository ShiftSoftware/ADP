using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Extensions;

public static class MenuModelBuilderExtensions
{
    public static ModelBuilder ConfigureMenuEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplacementItem>(x =>
        {
            x.HasIndex(x => x.Name).IsUnique()
                .HasFilter($"{nameof(ReplacementItem.IsDeleted)} = 0");
        });

        modelBuilder.Entity<VehicleModelLabourRate>(x =>
        {
            x.HasOne(x => x.VehicleModel)
                .WithMany(x => x.LabourRates)
                .HasForeignKey(x => x.VehicleModelID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.VehicleModelID, x.CountryID })
                .IsUnique()
                .HasFilter($"{nameof(VehicleModelLabourRate.IsDeleted)} = 0");
        });

        modelBuilder.Entity<ReplacementItemVehicleModel>(x =>
        {
            x.HasOne(x => x.ReplacementItem)
                .WithMany(x => x.ReplacementItemVehicleModels)
                .HasForeignKey(x => x.ReplacementItemID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.VehicleModel)
                .WithMany(x => x.ReplacementItemVehicleModels)
                .HasForeignKey(x => x.VehicleModelID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReplacementItemVehicleModelPart>(x =>
        {
            x.HasOne(x => x.ReplacementItemVehicleModel)
                .WithMany(x => x.DefaultParts)
                .HasForeignKey(x => x.ReplacementItemVehicleModelID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.ReplacementItemVehicleModelID, x.SortOrder })
                .IsUnique()
                .HasFilter($"{nameof(ReplacementItemVehicleModelPart.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuItem>(x =>
        {
            x.HasOne(x => x.MenuVariant)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.MenuVariantID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.ReplacementItemVehicleModel)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.ReplacementItemVehicleModelID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.MenuVariantID, x.ReplacementItemVehicleModelID })
                .IsUnique()
                .HasFilter($"{nameof(MenuItem.IsDeleted)} = 0 AND {nameof(MenuItem.ReplacementItemVehicleModelID)} IS NOT NULL");
        });

        modelBuilder.Entity<MenuItemPart>(x =>
        {
            x.HasOne(x => x.MenuItem)
                .WithMany(x => x.Parts)
                .HasForeignKey(x => x.MenuItemID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.MenuItemID, x.SortOrder })
                .IsUnique()
                .HasFilter($"{nameof(MenuItemPart.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuItemPartCountryPrice>(x =>
        {
            x.HasOne(x => x.MenuItemPart)
                .WithMany(x => x.CountryPrices)
                .HasForeignKey(x => x.MenuItemPartID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.MenuItemPartID, x.CountryID })
                .IsUnique()
                .HasFilter($"{nameof(MenuItemPartCountryPrice.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuEntity>(x =>
        {
            x.HasOne(x => x.VehicleModel)
                .WithMany()
                .HasForeignKey(x => x.VehicleModelID)
                .OnDelete(DeleteBehavior.Restrict);

            x.HasIndex(x => x.BasicModelCode).IsUnique()
                .HasFilter($"{nameof(MenuEntity.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuVariant>(x =>
        {
            x.HasOne(x => x.Menu)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.MenuID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.MenuID, x.MenuPrefix, x.MenuPostfix }).IsUnique()
                .HasFilter($"{nameof(MenuVariant.IsDeleted)} = 0");

            x.HasIndex(x => new { x.MenuID, x.Name }).IsUnique()
                .HasFilter($"{nameof(MenuVariant.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuVariantLabourRate>(x =>
        {
            x.HasOne(x => x.MenuVariant)
                .WithMany(x => x.LabourRates)
                .HasForeignKey(x => x.MenuVariantID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(x => new { x.MenuVariantID, x.CountryID })
                .IsUnique()
                .HasFilter($"{nameof(MenuVariantLabourRate.IsDeleted)} = 0");
        });

        modelBuilder.Entity<MenuVersion>(x =>
        {
            x.HasIndex(x => x.Version).IsUnique();
        });

        modelBuilder.Entity<ServiceIntervalGroup>();

        modelBuilder.Entity<ReplacementItemServiceIntervalGroup>(x =>
        {
            x.HasOne(x => x.ReplacementItem)
                .WithMany(x => x.ReplacementItemServiceIntervalGroups)
                .HasForeignKey(x => x.ReplacementItemID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.ServiceIntervalGroup)
                .WithMany(x => x.ReplacementItemServiceIntervalGroups)
                .HasForeignKey(x => x.ServiceIntervalGroupID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VehicleModelLabourDetails>(x =>
        {
            x.HasOne(x => x.VehicleModel)
                .WithMany(x => x.LabourDetails)
                .HasForeignKey(x => x.VehicleModelID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.ServiceIntervalGroup)
                .WithMany(x => x.VehicleModelLabourDetails)
                .HasForeignKey(x => x.ServiceIntervalGroupID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuPeriodicAvailability>(x =>
        {
            x.HasOne(x => x.MenuVariant)
                .WithMany(x => x.PeriodicAvailabilities)
                .HasForeignKey(x => x.MenuVariantID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.ServiceInterval)
                .WithMany(x => x.MenuPeriodicAvailabilities)
                .HasForeignKey(x => x.ServiceIntervalID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuLabourDetails>(x =>
        {
            x.HasOne(x => x.MenuVariant)
                .WithMany(x => x.LabourDetails)
                .HasForeignKey(x => x.MenuVariantID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasOne(x => x.ServiceIntervalGroup)
                .WithMany(x => x.MenuLabourDetails)
                .HasForeignKey(x => x.ServiceIntervalGroupID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabourRateMapping>(x =>
        {
            x.HasIndex(p => new { p.BrandID, p.LabourRate }).IsUnique()
                .HasFilter($"{nameof(LabourRateMapping.IsDeleted)} = 0");
        });

        modelBuilder.Entity<BrandMapping>(x =>
        {
            x.HasIndex(p => p.BrandID).IsUnique()
                .HasFilter($"{nameof(BrandMapping.IsDeleted)} = 0");
        });

        // Place every entity owned by the Menu package under the "Menu" SQL schema,
        // including its temporal history table.
        var menuAssembly = typeof(MenuModelBuilderExtensions).Assembly;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.Assembly != menuAssembly)
                continue;

            entityType.SetSchema("Menu");
            entityType.SetHistoryTableSchema("Menu");
        }

        return modelBuilder;
    }
}
