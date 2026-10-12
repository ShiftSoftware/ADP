using Microsoft.Data.SqlClient;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>Tests that set the engine's process-wide <c>DARLASTIC_*</c> variables run alone.</summary>
[CollectionDefinition(nameof(RegistryEnvironmentCollection), DisableParallelization = true)]
public sealed class RegistryEnvironmentCollection;

/// <summary>
/// The resolve's <c>Darlastic.Resolve</c> applock against a real SQL Server — opt-in: set <c>DARLASTIC_TEST_SQL</c> to
/// a connection string for a scratch database nobody else uses (the test bootstraps a registry in it and writes one
/// empty resolve run). Unset, skipped. The lock is Session-owned: it must end with the resolve, and with a check that
/// fails right after it is taken, on a pooled connection as on an unpooled one — otherwise every later resolve in the
/// process reads "another resolve is already running".
/// </summary>
[Collection(nameof(RegistryEnvironmentCollection))]
public sealed class RegistryResolveLockTests
{
    private const string Variable = "DARLASTIC_TEST_SQL";
    private static readonly string[] EngineVariables = ["DARLASTIC_SQL", "DARLASTIC_DB", "DARLASTIC_SCHEMA_MANAGED", "DARLASTIC_TENANT", "DARLASTIC_FORCE"];

    private static bool LockFree(string sql)
    {
        using var probe = new SqlConnection(new SqlConnectionStringBuilder(sql) { Pooling = false }.ConnectionString);
        probe.Open();
        using var cmd = new SqlCommand("SELECT APPLOCK_TEST('public', 'Darlastic.Resolve', 'Exclusive', 'Session')", probe);
        return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
    }

    private static void WithEngineEnvironment(string sql, string tenant, Action body)
    {
        var saved = EngineVariables.ToDictionary(v => v, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var v in EngineVariables) Environment.SetEnvironmentVariable(v, null);
            Environment.SetEnvironmentVariable("DARLASTIC_SQL", sql);
            Environment.SetEnvironmentVariable("DARLASTIC_TENANT", tenant);
            body();
        }
        finally
        {
            foreach (var (v, value) in saved) Environment.SetEnvironmentVariable(v, value);
        }
    }

    private const string Tenant = "synthetic-a"; // stable: the scratch registry keeps its tenant marker across runs

    /// <summary>A pool of its own for each test (pools are keyed by the exact string), and its unpooled twin.</summary>
    private static (string Pooled, string Unpooled) Strings()
    {
        var configured = Environment.GetEnvironmentVariable(Variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(configured), $"{Variable} is not set (opt-in SQL Server test).");
        var pooled = new SqlConnectionStringBuilder(configured) { Pooling = true, ApplicationName = "darlastic-locktest-" + Guid.NewGuid().ToString("N") }.ConnectionString;
        return (pooled, new SqlConnectionStringBuilder(pooled) { Pooling = false }.ConnectionString);
    }

    [Fact]
    public void ASuccessfulResolve_ReleasesTheLock_EvenOnAPooledConnection()
    {
        var (pooled, _) = Strings();
        try
        {
            // The connection goes back to the pool with its session alive; the lock must not stay with it.
            WithEngineEnvironment(pooled, Tenant, () => Registry.RunResolve([], new Merge.Result()));
            Assert.True(LockFree(pooled), "the lock outlived a successful resolve on a pooled connection");
            WithEngineEnvironment(pooled, Tenant, () => Registry.RunResolve([], new Merge.Result()));
            Assert.True(LockFree(pooled));
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACheckThatFailsRightAfterTheLock_ReleasesIt(bool pooledConnection)
    {
        var (pooled, unpooled) = Strings();
        string sql = pooledConnection ? pooled : unpooled;
        try
        {
            // The registry exists and belongs to its tenant (bootstrapped unpooled: this step does not depend on the fix).
            WithEngineEnvironment(unpooled, Tenant, () => Registry.RunResolve([], new Merge.Result()));

            // A check after the lock fails: the tenant assert (the registry belongs to another tenant).
            WithEngineEnvironment(sql, "synthetic-b", () =>
            {
                var first = Assert.Throws<InvalidOperationException>(() => Registry.RunResolve([], new Merge.Result()));
                Assert.Contains("Refusing to touch it", first.Message);
                Assert.True(LockFree(sql), "the lock outlived a failed open check");
                // The next resolve meets the same refusal, not "another resolve is already running".
                var second = Assert.Throws<InvalidOperationException>(() => Registry.RunResolve([], new Merge.Result()));
                Assert.Contains("Refusing to touch it", second.Message);
            });
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }
}
