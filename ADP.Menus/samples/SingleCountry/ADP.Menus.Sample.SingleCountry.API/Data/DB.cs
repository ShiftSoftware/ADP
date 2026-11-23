using ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Data;

/// <summary>
/// The sample's own database: the Todo items, plus the menu entities the Menus module adds through its
/// <c>IModelBuildingContributor</c>. Users, companies and countries live on the external identity server.
/// </summary>
public class DB : ShiftDbContext
{
    public DbSet<TodoItem> TodoItems { get; set; } = default!;

    public DB(DbContextOptions<DB> options) : base(options)
    {
    }
}
