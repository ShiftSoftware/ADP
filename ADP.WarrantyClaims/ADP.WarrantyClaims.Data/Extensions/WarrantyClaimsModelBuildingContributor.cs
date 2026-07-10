using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;

/// <summary>
/// Contributes the warranty entity configuration to the consumer's <see cref="ShiftDbContext"/> at
/// model-build time, so the consumer does not have to call <c>ConfigureWarrantyClaimsEntities()</c>
/// from its own <c>OnModelCreating</c>. The target schema is captured at registration time from the
/// API options.
/// </summary>
public class WarrantyClaimsModelBuildingContributor : IModelBuildingContributor
{
    private readonly string? schema;

    public WarrantyClaimsModelBuildingContributor(string? schema)
    {
        this.schema = schema;
    }

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureWarrantyClaimsEntities(schema);
    }
}
