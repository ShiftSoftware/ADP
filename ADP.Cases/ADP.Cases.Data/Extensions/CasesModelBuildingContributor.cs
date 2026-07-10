using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Cases.Data.Extensions;

/// <summary>
/// Contributes the ADP.Cases entities to the consumer's DbContext at model-build time
/// (register as a singleton <see cref="IModelBuildingContributor"/>). Same pattern as
/// ADP.ClaimableItems' contributor. See <see cref="CasesModelBuilderExtensions.ConfigureCasesEntities"/>
/// for when this is needed vs a consumer-declared DbSet.
/// </summary>
public class CasesModelBuildingContributor : IModelBuildingContributor
{
    private readonly string? schema;

    public CasesModelBuildingContributor(string? schema = "Cases")
    {
        this.schema = schema;
    }

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureCasesEntities(schema);
    }
}
