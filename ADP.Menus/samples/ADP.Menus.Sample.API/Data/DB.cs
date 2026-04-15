using ShiftSoftware.ADP.Menus.Sample.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;

namespace ShiftSoftware.ADP.Menus.Sample.API.Data;

public class DB : ShiftIdentityDbContext
{
    public DbSet<TodoItem> TodoItems { get; set; } = default!;

    public DB(DbContextOptions<DB> options) : base(options)
    {
    }
}
