using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data;

/// <summary>
/// Sample consumer DbContext. Inherits <see cref="ShiftIdentityDbContext"/> to pick up
/// the identity tables (Users, Roles, Companies, Brands, etc.) and gets Surveys entities
/// added via the <c>IModelBuildingContributor</c> registered by <c>AddSurveysApiServices</c>.
/// Sample ships with no consumer-side entities beyond what the Surveys module provides.
/// </summary>
public class DB : ShiftIdentityDbContext
{
    public DB(DbContextOptions<DB> options) : base(options)
    {
    }
}
