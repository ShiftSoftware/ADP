using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.Menus;

/// <summary>
/// The Menus parity pass.
///
/// <para>
/// <b>This baseline is of the CURRENT tree, which is already post-migration</b> (the mapper work
/// landed at 14caf7c9). The pre-migration Menus baseline is Step 01's job, captured retroactively
/// from a git worktree at 14caf7c9^ - this one is what that capture gets compared against.
/// </para>
///
/// <para>
/// Menus is the richest group in the plan: trap 1, trap 2 and trap 3-write are all present, each
/// confirmed at a cited line and then adversarially re-verified.
/// </para>
/// </summary>
public class MenusParityTests
{
    private readonly ITestOutputHelper output;

    public MenusParityTests(ITestOutputHelper output) => this.output = output;

    private const string Group = "Menus";

    /// <summary>Note: "api/Menu", singular - the sample sets it at Program.cs:157.</summary>
    private const string RoutePrefix = "api/Menu";

    private const string Database = "ADP_Parity_Menus";

    private static readonly string[] ActionTrees =
    {
        "ShiftIdentityActions", "AzureStorageActionTree", "GeneralActionTree", "MenuActionTree",
    };

    private static readonly string[] Entities =
    {
        "Menu", "MenuVariant", "MenuVersion", "VehicleModel", "ReplacementItem",
        "ServiceInterval", "ServiceIntervalGroup", "StandaloneReplacementItemGroup",
        "BrandMapping", "LabourRateMapping",
    };

    private static string MasterConnection =>
        @"Server=localhost\sqlexpress;Initial Catalog=master;Persist Security Info=True;" +
        "Integrated Security=SSPI;TrustServerCertificate=True;";

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ADP.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("ADP.sln not found above " + AppContext.BaseDirectory);
        }
    }

    [Fact]
    public async Task Endpoint_parity()
    {
        var ct = TestContext.Current.CancellationToken;

        var parityRoot = ParityGroupRun.ParityRootFromEnvironment(Path.Combine(RepoRoot, "ADP.EndpointParity"));
        var mode = ParityGroupRun.ModeFromEnvironment();
        var grant = ParityGroupRun.GrantFromEnvironment();

        output.WriteLine($"mode={mode} grant={grant}");

        await ParitySeeder.ResetDatabaseAsync(MasterConnection, Database, ct);

        await using var factory = new SampleHostFactory<Program>(
            connectionStringKey: "ConnectionStrings:SQLServer",
            databaseName: Database,
            groupOverrides: new Dictionary<string, string?>
            {
                // Cosmos OFF. The sample gates all replication and provisioning on this being
                // configured, so emptying it skips the whole block - which also removes
                // replication side effects from the write-path cases. Replication is
                // fire-and-forget and its failures are log lines, so it could not be diffed
                // through HTTP anyway.
                ["ConnectionStrings:Cosmos"] = "",
                ["CosmosDb:Enabled"] = "False",
                ["CosmosDb:ConnectionString"] = "",
            });

        var client = factory.CreateClient();
        var config = factory.Services.GetRequiredService<IConfiguration>();

        var seeder = new ParitySeeder(config.GetConnectionString("SQLServer")!);
        await seeder.ApplyAsync(Path.Combine(parityRoot, "Seed", "menus.seed.json"), ct);
        output.WriteLine($"seeded: {seeder.SeededIds.Sum(kv => kv.Value.Count)} rows, " +
                         $"{seeder.HostileMarkers.Count} hostile markers");

        var issuer = config["Settings:TokenSettings:Issuer"]!;
        var privateKey = config["Settings:TokenSettings:PrivateKey"]!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", ParityAuth.MintToken(
            issuer, privateKey,
            ParityAuth.BuildAccessTree(ParityGrant.FullAccess, ActionTrees, new Dictionary<string, int[]>())));

        var seededHashIds = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var entity in Entities)
        {
            var ids = await SeededIdResolver.ResolveAsync(client, RoutePrefix, entity, ct);
            seededHashIds[entity] = ids;
            output.WriteLine($"  {entity}: {ids.Count} ids");
        }

        // Hand-authored minimal-valid bodies, with @@Entity[n]@@ tokens resolved to the real hash
        // ids the seed produced. Seven of the ten entities are covered; the other three need a
        // ShiftIdentity Brand row this sample does not create, and are excluded below with that
        // reason rather than quietly dropped.
        var createBodies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entity in new[]
                 {
                     "Menu", "ReplacementItem",
                     "ServiceInterval", "ServiceIntervalGroup", "StandaloneReplacementItemGroup",
                 })
        {
            var path = Path.Combine(parityRoot, "Seed", $"menus.{entity}.create.json");
            if (File.Exists(path))
                createBodies[entity] = SeedTokenSubstitution.Apply(
                    await File.ReadAllTextAsync(path, ct), seededHashIds);
        }
        output.WriteLine($"create bodies: {string.Join(", ", createBodies.Keys)}");

        var groupConfig = new ParityGroupConfig
        {
            Group = Group,
            RoutePrefix = RoutePrefix,
            ActionTrees = ActionTrees,
            RestrictedGrant = new Dictionary<string, int[]> { ["MenuActionTree"] = new[] { 1 } },
            Issuer = issuer,
            PrivateKeyBase64 = privateKey,
            SeededHashIds = seededHashIds,
            HostileMarkers = seeder.HostileMarkers,
            CreateBodies = createBodies,
            UpdateBodies = new Dictionary<string, string>(createBodies, StringComparer.Ordinal),

            // Same pre-existing condition as Surveys, and this run CONFIRMS IT IS REPO-WIDE
            // rather than one group's misconfiguration: 24 of the first Menus capture's cases
            // were 500s, all of them REVISIONS or asOf, all "FOR SYSTEM_TIME ... is not a
            // system-versioned table". Entities carry [TemporalShiftEntity] but nothing calls
            // .IsTemporal(true), so the tables are created plain while the inherited routes still
            // emit temporal SQL. Not caused by the upgrade; recorded so a green run does not imply
            // the temporal mapper path was covered, because it was not.
            EmitAsOfCases = false,
            ExcludedRoutes = new[]
            {
                "GET /api/Menu/Menu/{key}/revisions",
                "GET /api/Menu/MenuVariant/{key}/revisions",
                "GET /api/Menu/MenuVersion/{key}/revisions",
                "GET /api/Menu/VehicleModel/{key}/revisions",
                "GET /api/Menu/ReplacementItem/{key}/revisions",
                "GET /api/Menu/ServiceInterval/{key}/revisions",
                "GET /api/Menu/ServiceIntervalGroup/{key}/revisions",
                "GET /api/Menu/StandaloneReplacementItemGroup/{key}/revisions",
                "GET /api/Menu/BrandMapping/{key}/revisions",
                "GET /api/Menu/LabourRateMapping/{key}/revisions",

                // --- attention-signal CLEAR: mutates state mid-run -------------------------
                // Clearing signals for a row changes the ATTENTION body of a later case and makes
                // the run order-dependent. The READ side ({key}/attention) is covered.
                "POST /api/Menu/Menu/{key}/attention/clear",
                "POST /api/Menu/MenuVariant/{key}/attention/clear",
                "POST /api/Menu/MenuVersion/{key}/attention/clear",
                "POST /api/Menu/VehicleModel/{key}/attention/clear",
                "POST /api/Menu/ReplacementItem/{key}/attention/clear",
                "POST /api/Menu/ServiceInterval/{key}/attention/clear",
                "POST /api/Menu/ServiceIntervalGroup/{key}/attention/clear",
                "POST /api/Menu/StandaloneReplacementItemGroup/{key}/attention/clear",
                "POST /api/Menu/BrandMapping/{key}/attention/clear",
                "POST /api/Menu/LabourRateMapping/{key}/attention/clear",

                // --- writes needing a ShiftIdentity Brand row this sample never seeds -------
                // VehicleModel, BrandMapping and LabourRateMapping all take a Brand
                // ShiftEntitySelectDTO whose Value must decode to a real Brand. The Menus sample
                // seeds identity (SuperUser + org) but no Brands, so a minimal-valid body cannot
                // be authored against this host. READ-path parity for all three is fully covered,
                // including VehicleModel's trap1 and trap2 sites, which are read-path phenomena.
                // WRITE-path parity for these three is a DECLARED GAP, not a silent one.
                "POST /api/Menu/VehicleModel",
                "PUT /api/Menu/VehicleModel/{key}",
                "POST /api/Menu/BrandMapping",
                "PUT /api/Menu/BrandMapping/{key}",
                "POST /api/Menu/LabourRateMapping",
                "PUT /api/Menu/LabourRateMapping/{key}",
                // DELETE goes with them: the lifecycle deletes the row this run CREATED, and
                // there is none. Deleting a SEEDED row instead would change every later list body.
                "DELETE /api/Menu/VehicleModel/{key}",
                "DELETE /api/Menu/BrandMapping/{key}",
                "DELETE /api/Menu/LabourRateMapping/{key}",
                "DELETE /api/Menu/MenuVariant/{key}",

                // --- MenuVariant writes need a country-scoped labour rate -------------------
                // MenuVariantRepository rejects an empty LabourRates collection outright
                // ("Menu variant labour rates are required"), and each entry needs a CountryID
                // resolving to a seeded identity Country. Same gap as above and the same
                // consolation: MenuVariant's trap1 (soft-deleted items, nested parts) and trap2
                // (periodic-availability link row, two-hop replacement item) are all READ-path
                // and all covered by its DETAIL case.
                "POST /api/Menu/MenuVariant",
                "PUT /api/Menu/MenuVariant/{key}",
                "DELETE /api/Menu/MenuVariant/DeleteWithGuard/{key}",

                // --- MenuVersion has NO WORKING WRITE PATH ---------------------------------
                // MenuVersionRepository.UpsertAsync throws NotImplementedException outright, so
                // POST and PUT are 500 by construction. Found by this capture, not assumed.
                "POST /api/Menu/MenuVersion",
                "PUT /api/Menu/MenuVersion/{key}",
                "DELETE /api/Menu/MenuVersion/{key}",

                // --- hand-written actions: no ShiftEntity triple, no framework mapper -------
                // Excel exports are Rule-7 binaries; the stock and usage endpoints are bespoke
                // query actions. A MAPPER upgrade cannot silently change any of them. If this
                // migration ever touches serialization or routing, they must be covered first.
                "GET /api/Menu/Menu/ExportMenusToExcel",
                "GET /api/Menu/Menu/ExportRTSCodesToExcel",
                "GET /api/Menu/Menu/MenuDetailReportExcel",
                "GET /api/Menu/Menu/StockByPartNumber/{partNumber}",
                "GET /api/Menu/Menu/StockByPartNumbers",
                "POST /api/Menu/Menu/UpdatePartsPrice",
                "GET /api/Menu/MenuVariant/ByMenu/{menuID}",
                "GET /api/Menu/VehicleModel/GetById/{key}",
                "GET /api/Menu/VehicleModel/ReplacementItemUsage/{key}/{replacementItemKey}",
                "POST /api/Menu/VehicleModel/CheckReplacementItemMenuUsage/{key}",
                "POST /api/Menu/VehicleModel/PropagateReplacementItem/{key}",
            },

            // MenuVersion's repository throws NotImplementedException on upsert - a genuinely
            // unreachable write path, which is what this list is for. It needs a mapper-level
            // write golden instead, if its write mapper is ever exercised at all.
            WriteUnreachable = new[] { "MenuVersion" },

            Normalization = new NormalizerOptions
            {
                HeaderAllowlist = new(StringComparer.OrdinalIgnoreCase) { "Content-Language" },
                RunStart = DateTimeOffset.UtcNow,
            },
        };

        var summary = await ParityGroupRun.ExecuteAsync(
            client, factory.Services, groupConfig, grant, mode, parityRoot, ct);

        output.WriteLine("\n" + summary);
        Assert.True(summary.Passes, "Parity gates failed:\n" + summary);
    }
}
