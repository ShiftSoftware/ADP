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
/// <param name="verification">Also add the verification tables: only for a host that turned verification on, so
/// every other host's model, and its migrations, stay as they are.</param>
public class DarlasticModelBuildingContributor(string? schema = "Darlastic", bool verification = false) : IModelBuildingContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureDarlasticEntities(schema);
        if (verification)
            modelBuilder.ConfigureDarlasticVerificationEntities(schema);
    }
}
