using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Extensions;

public static class ClaimableItemsModelBuilderExtensions
{
    /// <summary>
    /// Configures the claimable-items catalog entities (ClaimableItem / Campaign /
    /// CampaignVinEntry) on the consumer's model. Populated in Slice 3.
    /// </summary>
    /// <param name="schema">
    /// SQL schema to place every module-owned entity (and its temporal history table) under.
    /// The sample host passes <c>"ClaimableItems"</c>; the Toyota Central Asia consumer passes
    /// <c>null</c> so the existing year-old <c>dbo</c> temporal tables are NOT renamed under
    /// SYSTEM_VERSIONING (see risk R1 / decision D3 in the extraction plan). When null the
    /// entities keep the model's default schema.
    /// </param>
    public static ModelBuilder ConfigureClaimableItemsEntities(this ModelBuilder modelBuilder, string? schema = "ClaimableItems")
    {
        // Register the catalog entities on the consumer's model. Relationships (Campaign<->ClaimableItem,
        // CampaignVinEntry->Campaign), filtered unique-hash indexes, and temporal history are applied by
        // ShiftEntity conventions from the [TemporalShiftEntity] attribute + IEntityHasUniqueHash interface,
        // exactly as when these entities lived in the consumer's own Data project.
        // NOTE: Campaign->VehicleInspectionType is intentionally NOT configured here — VehicleInspectionType
        // is a consumer-owned type; the consumer keeps the principal-side navigation, which preserves the FK.
        modelBuilder.Entity<ClaimableItem>();
        modelBuilder.Entity<Campaign>();
        modelBuilder.Entity<CampaignVinEntry>();

        if (!string.IsNullOrWhiteSpace(schema))
        {
            var assembly = typeof(ClaimableItemsModelBuilderExtensions).Assembly;
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
