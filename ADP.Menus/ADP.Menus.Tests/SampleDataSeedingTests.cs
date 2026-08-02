using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Tests.SampleSeeding;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Loads the sample database's demo data — the seeding the sample API used to do at startup, now a thing
/// you trigger:
///
///     dotnet test ADP.Menus/ADP.Menus.Tests --filter SampleDataSeeding
///
/// <para><b>Why it moved.</b> Seeding at boot wrote demo rows silently, and the replication trigger
/// copied them into Cosmos. Getting rid of them again is the problem: the dev data import empties the
/// <c>[Menu]</c> schema in raw SQL, so no trigger fires and the documents are left behind — a basic model
/// code the lookup keeps serving that is nowhere in the database. Demo data belongs somewhere you invoke
/// on purpose, against a database you chose.</para>
///
/// <para><b>It only ever adds.</b> Every row is matched first — the catalogue on its authored id, the
/// menu graph on its natural key — so it fills in what is missing and leaves everything else exactly as
/// it is. Run it on an empty database and you get the whole demo catalogue; run it after a dev data
/// import and you get the demo rows that import did not bring; run it twice and the second run inserts
/// nothing. It never updates a row, which is what makes it safe to point at a database holding real
/// imported data.</para>
///
/// <para>Targets the sample's own local database by default, so with SQL Express running and the sample
/// API started once (it creates the schema), the filter above is all it takes. Point it elsewhere with:</para>
///
/// <code>
/// dotnet test ADP.Menus/ADP.Menus.Tests --filter SampleDataSeeding ^
///     -e ADP_MENUS_SAMPLE_SQL_CONNECTION="Server=...;Initial Catalog=...;"
/// </code>
///
/// <para>Like <see cref="ServiceMenusProvisioningTests"/> — and unlike every other test here, which are
/// pure and offline — this talks to a real database, so it SKIPS rather than fails when none is
/// reachable. CI stays green without one.</para>
///
/// <para><b>Cosmos does not follow.</b> The seeding context registers no replication trigger, so the rows
/// land in SQL marked never-replicated. The Functions host's <c>POST api/replicate-all</c> is what puts
/// them in Cosmos, and it is deliberately a separate step.</para>
/// </summary>
public class SampleDataSeedingTests
{
    private const string ConnectionVariable = "ADP_MENUS_SAMPLE_SQL_CONNECTION";

    /// <summary>
    /// The sample API's own <c>ConnectionStrings:SQLServer</c> from appsettings.Development.json. A local
    /// developer database with integrated security — not a credential.
    /// </summary>
    private const string SampleConnectionString =
        "Server=localhost\\sqlexpress;Initial Catalog=MenuSample;Persist Security Info=True;"
        + "Integrated Security=SSPI;TrustServerCertificate=True;";

    /// <summary>Kept short so a database that is not there skips promptly instead of stalling a test run.</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SeedsEveryDemoRowTheSampleDatabaseIsMissing()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = SampleConnectionString;

        await using var db = Open(connectionString);

        // The schema is the sample API's to create (it calls EnsureCreatedAsync at startup). Seeding into
        // a database that does not exist yet would be the wrong kind of helpful, so say so and skip.
        try
        {
            if (!await db.Database.CanConnectAsync(TestContext.Current.CancellationToken))
            {
                Assert.Skip(Unreachable(connectionString, "the database does not exist or is not reachable"));
                return;
            }
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or TaskCanceledException)
        {
            Assert.Skip(Unreachable(connectionString, exception.Message));
            return;
        }

        var report = await SampleSeedData.SeedMissingAsync(db, TestContext.Current.CancellationToken);

        foreach (var table in report.Tables)
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{table.Table}: inserted {table.Inserted}, already present {table.AlreadyPresent}");

        TestContext.Current.TestOutputHelper?.WriteLine($"Total inserted: {report.Inserted}");

        // The seed is complete afterwards however much of it was already there — that is the whole point
        // of matching every row rather than guarding on "is the table empty".
        var codes = await db.Set<Data.Entities.Menu>()
            .Where(menu => SampleSeedData.DemoBasicModelCodes.Contains(menu.BasicModelCode))
            .Select(menu => menu.BasicModelCode)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            SampleSeedData.DemoBasicModelCodes.OrderBy(code => code, StringComparer.Ordinal),
            codes.Distinct().OrderBy(code => code, StringComparer.Ordinal));
    }

    /// <summary>
    /// Running it again must insert nothing. This is the property that lets it be pointed at a database
    /// with real imported data in it: a second pass is a no-op, so it cannot accumulate duplicates or
    /// walk over anything.
    /// </summary>
    [Fact]
    public async Task SeedingTwiceInsertsNothingTheSecondTime()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = SampleConnectionString;

        await using var db = Open(connectionString);

        try
        {
            if (!await db.Database.CanConnectAsync(TestContext.Current.CancellationToken))
            {
                Assert.Skip(Unreachable(connectionString, "the database does not exist or is not reachable"));
                return;
            }
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or TaskCanceledException)
        {
            Assert.Skip(Unreachable(connectionString, exception.Message));
            return;
        }

        await SampleSeedData.SeedMissingAsync(db, TestContext.Current.CancellationToken);

        // A fresh context, so nothing is answered from the first one's change tracker.
        await using var second = Open(connectionString);
        var report = await SampleSeedData.SeedMissingAsync(second, TestContext.Current.CancellationToken);

        Assert.True(
            report.NothingToDo,
            "A second seeding pass inserted "
            + string.Join(", ", report.Tables.Where(table => table.Inserted > 0).Select(table => $"{table.Inserted} {table.Table}"))
            + " — every row must be matched before it is written.");
    }

    private static SampleSeedDB Open(string connectionString) =>
        new(new DbContextOptionsBuilder<SampleSeedDB>()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout((int)ConnectTimeout.TotalSeconds))
            .Options);

    private static string Unreachable(string connectionString, string reason) =>
        $"Could not reach {(connectionString == SampleConnectionString ? "the sample database" : "the database in " + ConnectionVariable)}: {reason}. "
        + "Start SQL Express and run the sample API once so it creates the schema, or set "
        + ConnectionVariable + " to another database.";
}
