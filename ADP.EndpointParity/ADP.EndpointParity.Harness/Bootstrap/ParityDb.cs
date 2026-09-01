using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// The mounted host's DbContext. Deliberately empty of entities of its own: the group's
/// <c>IModelBuildingContributor</c>, registered by its own
/// <c>Add&lt;Group&gt;ApiServices&lt;TDbContext&gt;</c> call, contributes the module's tables at
/// model-build time - which is exactly what a real tenant host gets.
///
/// <para>
/// Same shape as <c>ADP.Darlastic.Sample.API/Data/SampleDB.cs</c>, which is the repo's own
/// precedent for "a consumer context that owns no entities".
/// </para>
///
/// <para>
/// <b>EnsureCreated, never migrations.</b> The modules ship no DDL and hosts own their schema.
/// Each parity run creates its own database and drops it afterwards, so there is no migration
/// history to maintain and no chance of two runs contaminating each other.
/// </para>
/// </summary>
public class ParityDb : ShiftDbContext
{
    public ParityDb(DbContextOptions<ParityDb> options) : base(options)
    {
    }
}
