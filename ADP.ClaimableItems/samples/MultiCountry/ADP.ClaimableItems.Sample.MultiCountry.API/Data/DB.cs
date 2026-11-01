using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Data;

/// <summary>
/// The sample's own database. It has no entities of its own: the Claimable Items and Cases modules add theirs
/// through the <c>IModelBuildingContributor</c>s registered in Program.cs. Users, companies and countries live on
/// the external identity server, so there are no identity tables here.
/// </summary>
public class DB : ShiftDbContext
{
    public DB(DbContextOptions<DB> options) : base(options)
    {
    }
}
