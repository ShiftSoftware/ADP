using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Extensions;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Sample.Functions;

/// <summary>
/// The menu tables, mapped exactly as the API host maps them — the minimum a sweep host needs.
///
/// <para><b>Why not <c>MenuDB</c>.</b> The module also ships <c>MenuDB</c>, which looks like the
/// obvious choice but is not interchangeable here: it declares <c>DbSet</c> properties
/// (<c>LabourRateMappings</c>, <c>MenuVariants</c>, …) and EF Core names a table after its DbSet
/// property when one exists. The sample API has no menu DbSets — it picks the entities up through
/// <c>MenuModelBuildingContributor</c> — so its tables are named after the ENTITY types
/// (<c>Menu.LabourRateMapping</c>, singular). Pointing <c>MenuDB</c> at that database fails with
/// "Invalid object name 'Menu.LabourRateMappings'".</para>
///
/// <para>Declaring no DbSets and calling <see cref="MenuModelBuilderExtensions.ConfigureMenuEntities"/>
/// directly reproduces the API's mapping exactly. The entity types enter the model through that
/// configuration and through navigation discovery, so <c>db.Set&lt;T&gt;()</c> works for every
/// replicated table without naming any of them here.</para>
///
/// <para>The general rule this illustrates: a sweep host must map the tables the SAME way the writing
/// host does. A second context over the same database is only safe when its model agrees.</para>
/// </summary>
public class MenuReplicationDB : ShiftDbContext
{
    public MenuReplicationDB(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureMenuEntities();
    }
}
