using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Boots a real sample API in-process over its own <c>Program</c>, with the things a parity run
/// needs and a developer run does not.
///
/// <para>
/// This is the strongest mode available - the actual host, actual middleware order, actual
/// routing, actual JSON options, actual auth. Where a group has no sample host,
/// <see cref="MountedHostFactory"/> is one notch weaker and says so in its own doc comment.
/// </para>
/// </summary>
public sealed class SampleHostFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string connectionStringKey;
    private readonly string databaseName;
    private readonly IReadOnlyDictionary<string, string?> groupOverrides;

    /// <param name="connectionStringKey">
    /// The configuration key holding this sample's SQL connection string, e.g.
    /// <c>ConnectionStrings:SQLServer</c>. Supplied by the group project because the samples do
    /// not agree on a name.
    /// </param>
    /// <param name="databaseName">
    /// Each run creates its own database (ADP_Parity_&lt;Group&gt;_&lt;runid&gt;) so runs cannot
    /// contaminate each other, and so Rule 1's "same longs both runs" is actually true rather
    /// than hoped for.
    /// </param>
    /// <param name="groupOverrides">
    /// Any further configuration this group needs neutralised - most importantly its own Cosmos
    /// keys. Cosmos is NOT required by any parity case: the samples gate all Cosmos work on the
    /// connection string being configured, so emptying it skips the whole replication +
    /// provisioning block. That is not merely convenient - it removes replication side effects
    /// from the write-path cases, and replication is fire-and-forget so its failures are log
    /// lines that could not be diffed through HTTP anyway. The keys differ per group (Menus uses
    /// ConnectionStrings:Cosmos; Surveys has no Cosmos at all), which is why they are passed in
    /// rather than guessed here.
    /// </param>
    public SampleHostFactory(
        string connectionStringKey,
        string databaseName,
        IReadOnlyDictionary<string, string?>? groupOverrides = null)
    {
        this.connectionStringKey = connectionStringKey;
        this.databaseName = databaseName;
        this.groupOverrides = groupOverrides ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                // A disposable database per run. EnsureCreated, no migrations.
                [connectionStringKey] =
                    "Server=localhost\\sqlexpress;Initial Catalog=" + databaseName +
                    ";Persist Security Info=True;Integrated Security=SSPI;TrustServerCertificate=True;",

                // Suppress the sample's own demo seeding (the parity branch added to the
                // sample's Program.cs). Without this the harness cannot tell "the adversarial
                // seed was applied" from "only demo data is present", and the demo rows'
                // identity-generated PKs make hash ids differ from run to run.
                ["Parity:SuppressSampleSeeding"] = "true",
            };

            foreach (var kv in groupOverrides) overrides[kv.Key] = kv.Value;

            config.AddInMemoryCollection(overrides);
        });
    }
}
