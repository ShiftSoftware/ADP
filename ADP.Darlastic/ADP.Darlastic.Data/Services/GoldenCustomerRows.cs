using Microsoft.EntityFrameworkCore;

namespace ShiftSoftware.ADP.Darlastic.Data.Services;

/// <summary>
/// The persisted golden-customer rows of a host that adopted them (<see cref="DarlasticViews.CreateGoldenCustomerRowsSql"/>):
/// the table the <c>GoldenCustomer</c> view then reads. The registry is the truth and the table a copy of it, so
/// the host refreshes it after every committed resolve, and whenever a resolve ran outside the host.
/// </summary>
public static class GoldenCustomerRows
{
    /// <summary>
    /// Refill the table from the registry, in one transaction. Returns the number of rows it now holds. Runs
    /// without a command timeout (15 to 40 s at ~960K goldens); the context's own timeout is put back after.
    /// </summary>
    public static async Task<int> RefreshAsync(DbContext db, string schema = "Darlastic", CancellationToken ct = default)
    {
        var timeout = db.Database.GetCommandTimeout();
        db.Database.SetCommandTimeout(0);
        try
        {
            await db.Database.ExecuteSqlRawAsync(DarlasticViews.RefreshGoldenCustomerRowsSql(schema), ct);
            return await db.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM [{schema}].[GoldenCustomerRow]").SingleAsync(ct);
        }
        finally
        {
            db.Database.SetCommandTimeout(timeout);
        }
    }
}
