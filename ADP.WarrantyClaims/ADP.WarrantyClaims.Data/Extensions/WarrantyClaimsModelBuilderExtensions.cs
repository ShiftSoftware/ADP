using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;

public static class WarrantyClaimsModelBuilderExtensions
{
    /// <summary>
    /// Registers the warranty entities (WarrantyClaim + labor/sublet/part lines +
    /// ManufacturerSettlmentSheet + AdditionalLaborOperationCode + WarrantyRates) on the consumer's model.
    /// Relationships (the line FKs, the WarrantyClaimSubletLine shadow FK, the WarrantyClaim ->
    /// Cases.Certificate / ManufacturerSettlmentSheet / self-ref navs), the filtered unique-hash index
    /// and temporal history are all discovered by ShiftEntity conventions from the
    /// [TemporalShiftEntity] attribute, IEntityHasUniqueHash, and the navigations — exactly as when
    /// these entities lived in the consumer's own Data project (empty-migration-diff gated).
    /// </summary>
    /// <param name="schema">
    /// SQL schema to place every module-owned entity (and its temporal history table) under. The original
    /// distributor consumer passes <c>null</c> so the existing year-old <c>dbo</c> temporal tables are
    /// NOT renamed under SYSTEM_VERSIONING. When null the entities keep the model's default schema.
    /// </param>
    public static ModelBuilder ConfigureWarrantyClaimsEntities(this ModelBuilder modelBuilder, string? schema = null)
    {
        modelBuilder.Entity<WarrantyClaim>();
        modelBuilder.Entity<WarrantyClaimLaborLine>();
        modelBuilder.Entity<WarrantyClaimSubletLine>();
        modelBuilder.Entity<WarrantyClaimPartLine>();
        modelBuilder.Entity<ManufacturerSettlmentSheet>();
        modelBuilder.Entity<AdditionalLaborOperationCode>();
        modelBuilder.Entity<WarrantyRates>();

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
