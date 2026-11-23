using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;

namespace ShiftSoftware.ADP.Identity.Sample.MultiCountry.API.Data;

public class DB : ShiftIdentityDbContext
{
    public DB(DbContextOptions<DB> options) : base(options)
    {
    }
}
