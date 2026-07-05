using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Extensions;

/// <summary>
/// Contributes the claimable-items catalog entity configuration to the consumer's
/// <see cref="ShiftDbContext"/> at model-build time, so the consumer does not have to call
/// <c>ConfigureClaimableItemsEntities()</c> from its own <c>OnModelCreating</c>.
/// The target schema is captured at registration time from the API options.
/// </summary>
public class ClaimableItemsModelBuildingContributor : IModelBuildingContributor
{
    private readonly string? schema;

    public ClaimableItemsModelBuildingContributor(string? schema)
    {
        this.schema = schema;
    }

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureClaimableItemsEntities(schema);
    }
}
