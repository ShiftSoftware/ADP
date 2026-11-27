using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;

public static class WarrantyClaimsModelBuilderExtensions
{
    /// <summary>
    /// Registers the warranty entities (WarrantyClaim + labor/sublet/part lines + status history +
    /// ManufacturerSettlmentSheet + AdditionalLaborOperationCode + WarrantyRates + WarrantyCurrencyRate + WarrantyLaborRate) on the consumer's model.
    /// Relationships (the line FKs, the WarrantyClaimSubletLine shadow FK, the WarrantyClaim ->
    /// Cases.Certificate / ManufacturerSettlmentSheet / self-ref navs), the filtered unique-hash index
    /// and temporal history are all discovered by ShiftEntity conventions from the
    /// [TemporalShiftEntity] attribute, IEntityHasUniqueHash, and the navigations.
    /// </summary>
    /// <param name="schema">
    /// SQL schema to place every module-owned entity (and its temporal history table) under. A host
    /// with existing <c>dbo</c> temporal tables passes <c>null</c> so they are
    /// NOT renamed under SYSTEM_VERSIONING. When null the entities keep the model's default schema.
    /// </param>
    public static ModelBuilder ConfigureWarrantyClaimsEntities(this ModelBuilder modelBuilder, string? schema = null)
    {
        // The column defaults make the host's migration turn every existing claim into a USD claim at
        // rate 1. WarrantyClaimsMigrationExtensions.MigrateWarrantyClaimsToUSD fills the USD equivalents.
        modelBuilder.Entity<WarrantyClaim>(claim =>
        {
            claim.Property(x => x.Currency).HasDefaultValue(DocumentCurrency.USD);
            claim.Property(x => x.CurrencyRate).HasDefaultValue(1m);
        });
        modelBuilder.Entity<WarrantyClaimLaborLine>();
        modelBuilder.Entity<WarrantyClaimSubletLine>();
        modelBuilder.Entity<WarrantyClaimPartLine>();
        modelBuilder.Entity<WarrantyClaimStatusHistory>();
        modelBuilder.Entity<ManufacturerSettlmentSheet>();
        modelBuilder.Entity<AdditionalLaborOperationCode>();
        modelBuilder.Entity<WarrantyRates>();
        modelBuilder.Entity<WarrantyCurrencyRate>();
        modelBuilder.Entity<WarrantyLaborRate>();

        if (!string.IsNullOrWhiteSpace(schema))
        {
            var assembly = typeof(WarrantyClaimsModelBuilderExtensions).Assembly;
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.Assembly != assembly)
                    continue;

                entityType.SetSchema(schema);
                entityType.SetHistoryTableSchema(schema);
            }
        }

        return modelBuilder;
    }
}
