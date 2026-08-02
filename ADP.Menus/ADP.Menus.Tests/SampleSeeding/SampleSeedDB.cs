using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Extensions;
using ShiftSoftware.ShiftIdentity.Data;

namespace ShiftSoftware.ADP.Menus.Tests.SampleSeeding;

/// <summary>
/// The sample database's tables, mapped exactly as the sample API maps them — the minimum the seeding
/// test needs to write into it.
///
/// <para><b>Why not the sample's own <c>DB</c>.</b> That context picks the menu entities up through
/// <c>MenuModelBuildingContributor</c>, which <c>ShiftDbContext</c> resolves from the APPLICATION service
/// provider. Constructed outside the API's DI container there are no contributors, so a <c>DB</c> built
/// here would have no menu entities in its model at all. Referencing the sample web app from a test
/// project to stand its whole container up would be worse than the duplication.</para>
///
/// <para>Subclassing <c>ShiftIdentityDbContext</c> (for <c>[ShiftIdentity].[Brands]</c>) and calling
/// <see cref="MenuModelBuilderExtensions.ConfigureMenuEntities"/> directly reproduces that model exactly.
/// This is the same move <c>MenuReplicationDB</c> makes in the sample Functions host, and for the same
/// reason: EF Core names a table after its <c>DbSet</c> property when one exists, so a context that
/// declares menu DbSets would look for differently-named tables. Declaring none and configuring the
/// entities directly gives the API's naming — <c>Menu.LabourRateMapping</c>, singular.</para>
///
/// <para>No replication trigger is registered on it, so seeding writes to SQL only. The rows land dirty
/// (<c>LastReplicationDate</c> null), which is exactly right: the Functions host's
/// <c>POST api/replicate-all</c> is what puts them in Cosmos, and it is a step you take deliberately.</para>
/// </summary>
public class SampleSeedDB : ShiftIdentityDbContext
{
    public SampleSeedDB(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureMenuEntities();
    }
}
