using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.Data.Extensions;

/// <summary>
/// Registered by the API package's <c>AddDarlasticApiServices</c> (host-DB mode) so consumer
/// DbContexts pick up the registry model without calling
/// <see cref="DarlasticModelBuilderExtensions.ConfigureDarlasticEntities"/> themselves.
/// Mirrors the Surveys/Menus contributor pattern; the schema is captured at registration
/// (ClaimableItems style — configurable, default "Darlastic").
/// </summary>
public class DarlasticModelBuildingContributor(string? schema = "Darlastic") : IModelBuildingContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureDarlasticEntities(schema);
    }
}
