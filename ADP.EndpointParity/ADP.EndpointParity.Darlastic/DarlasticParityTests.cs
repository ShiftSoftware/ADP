using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Darlastic.Sample.API.Data;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.Darlastic;

/// <summary>
/// The Darlastic pass — SPIKE-5, and the plan's ONLY framework-only control.
///
/// <para>
/// <b>This is a SMOKE result, not a value-parity result, and it must never be recorded as one.</b>
/// Darlastic has 0 repository triples and 0 AutoMapper profiles, so there is no mapping behaviour
/// here to regress. A green run proves the routes still exist and still respond; it proves nothing
/// about mapping.
/// </para>
///
/// <para>
/// <b>What makes it worth capturing anyway:</b> because nothing mapper-shaped exists here, a diff
/// in this group is unambiguously caused by the FRAMEWORK. Every other group's diff confounds two
/// causes — the framework change and the mapper rewrite — because
/// <c>ShiftEntityMapperValidation</c> throws at startup for a triple without a mapper, so no mapper
/// group can ever be captured in a "bumped but not migrated" state. This group is the control that
/// lets Steps 03-05 attribute their diffs.
/// </para>
///
/// <para>
/// SPIKE-5's two recorded blockers, and how each is handled here:
/// (1) <c>Program.cs</c> <c>return 1</c>s before <c>app.Run()</c> when <c>ConnectionStrings:Registry</c>
///     or <c>Sample:AllowDevAuth</c> are missing — both are injected as configuration below;
/// (2) it needs a registry database the repo does not seed, and <c>SampleDB</c> deliberately never
///     calls <c>EnsureCreated</c> (its own remarks explain why: the module ships no DDL and a second
///     schema authority against a real registry is the failure its engine guards against). The
///     harness therefore creates a DISPOSABLE schema itself, before the host boots, from the very
///     model contributor <c>AddDarlasticApiServices</c> registers.
/// </para>
/// </summary>
public class DarlasticParityTests
{
    private readonly ITestOutputHelper output;

    public DarlasticParityTests(ITestOutputHelper output) => this.output = output;

    private const string Group = "Darlastic";
    private const string RoutePrefix = "api/Darlastic";
    private const string Database = "ADP_Parity_Darlastic";

    private static string Connection(string database) =>
        $@"Server=localhost\sqlexpress;Initial Catalog={database};Persist Security Info=True;" +
        "Integrated Security=SSPI;TrustServerCertificate=True;";

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ADP.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("ADP.sln not found");
        }
    }

    [Fact]
    public async Task Endpoint_smoke()
    {
        var ct = TestContext.Current.CancellationToken;

        var parityRoot = ParityGroupRun.ParityRootFromEnvironment(Path.Combine(RepoRoot, "ADP.EndpointParity"));
        var mode = ParityGroupRun.ModeFromEnvironment();
        var grant = ParityGroupRun.GrantFromEnvironment();
        output.WriteLine($"mode={mode} grant={grant}");

        await ParitySeeder.ResetDatabaseAsync(Connection("master"), Database, ct);

        // SPIKE-5 blocker 1, and it needs ENVIRONMENT VARIABLES rather than the usual config
        // injection. This host reads ConnectionStrings:Registry and Sample:AllowDevAuth at the TOP
        // of Program.cs - before builder.Build(), and therefore before WebApplicationFactory's
        // ConfigureAppConfiguration callbacks run. An in-memory override arrives too late: the
        // first capture attempt still connected to the appsettings database and every controller
        // 500'd with "Cannot open database ... login failed".
        //
        // WebApplication.CreateBuilder reads environment variables into Configuration from the
        // start, and they outrank appsettings, so setting them here is what actually redirects the
        // host. Double underscore is the section separator.
        Environment.SetEnvironmentVariable("ConnectionStrings__Registry", Connection(Database));
        Environment.SetEnvironmentVariable("Sample__AllowDevAuth", "true");

        await using var factory = new SampleHostFactory<Program>(
            connectionStringKey: "ConnectionStrings:Registry",
            databaseName: Database);

        var client = factory.CreateClient();

        // SPIKE-5 blocker 2: create the registry schema the sample refuses to create for itself
        // (SampleDB's own remarks explain why it must not: the module ships no DDL, and a second
        // schema authority against a real registry is the failure the engine's
        // DARLASTIC_SCHEMA_MANAGED switch exists to prevent). A disposable parity database has no
        // such authority to conflict with, so the harness creates it.
        //
        // Taken from the HOST's provider, not a hand-built DbContextOptions: the Darlastic tables
        // reach the model through the IModelBuildingContributor that AddDarlasticApiServices
        // registers, so a context built outside DI has an EMPTY model and EnsureCreated silently
        // creates nothing. The first attempt did exactly that and every query then failed with
        // "Invalid object name 'Darlastic.GoldenCustomer'".
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDB>();
            await db.Database.EnsureCreatedAsync(ct);

            // EnsureCreated does not create VIEWS. GoldenCustomer is mapped with ToView
            // (DarlasticModelBuilderExtensions.cs:194) precisely so it stays out of table
            // migrations, and the module ships the DDL for hosts to run themselves
            // (DarlasticViews.CreateGoldenCustomerSql). Running it here is exactly what a host
            // migration does - without it GET GoldenCustomer 500s with
            // "Invalid object name 'Darlastic.GoldenCustomer'", which is a missing-view problem
            // masquerading as an endpoint failure.
            await db.Database.ExecuteSqlRawAsync(
                ShiftSoftware.ADP.Darlastic.Data.DarlasticViews.CreateGoldenCustomerSql(), ct);

            output.WriteLine("registry schema + golden view created from the host's model (disposable)");
        }

        // No token is minted: this host authenticates every request through its own
        // DevAuthenticationHandler, which is the whole reason it refuses to start without
        // Sample:AllowDevAuth. There is no identity server here and no action tree registered
        // (Program.cs calls AddTypeAuth(_ => { })), so both grants see the same surface - recorded
        // rather than dressed up as a privilege control.
        var groupConfig = new ParityGroupConfig
        {
            Group = Group,
            RoutePrefix = RoutePrefix,
            ActionTrees = Array.Empty<string>(),
            RestrictedGrant = new Dictionary<string, int[]>(),
            Issuer = "ADP.Parity",
            PrivateKeyBase64 = "",      // empty => no token minted, see ParityGroupRun

            EmitAsOfCases = false,

            // This host authenticates through DevAuthenticationHandler, whose principal carries no
            // data-level claims, so the framework applies its lowest page-size cap.
            FullAccessListTop = 5,
            RestrictedListTop = 5,

            ExcludedRoutes = new[]
            {
                // CaseBrowserUi serves the browser PAGE, not an API surface - it answers
                // text/html by design, which the global no-HTML assertion (correctly) refuses.
                // Excluding it is the honest treatment: it is a UI route, and this harness makes
                // no claim about rendered pages.
                "GET /api/Darlastic/CaseBrowserUi",
                "GET /api/Darlastic/CaseBrowserUi/{*path}",

                // ---- the 28 HAND-WRITTEN actions -------------------------------------------
                // This group declares ZERO ShiftEntity triples, so the templated CRUD case list
                // reaches almost nothing here: these are bespoke ControllerBase actions taking
                // query parameters, ids and POST bodies over registry state the parity database
                // does not contain.
                //
                // They are listed one by one rather than waved off in bulk BECAUSE that is what
                // the coverage gate is for - verification.md section 10 says Darlastic's
                // hand-written actions "exist only because excludedRoutes forces someone to write
                // down why they are not covered". This is that writing-down.
                //
                // WHAT IS LOST: nothing mapper-shaped. A MAPPER upgrade cannot silently change a
                // hand-written action - there is no mapper in the path. What IS covered, and what
                // makes this group the plan's framework-only control, is the ROUTE CATALOGUE
                // (40 routes, captured as its own golden) plus live value captures of the three
                // endpoints that serve framework-shaped responses: GoldenCustomer (an OData
                // envelope over a ToView entity), StewardQueue, and the catalogue itself. A
                // framework change to serialization, the OData envelope or ProblemDetails shape
                // shows up there.
                "DELETE /api/Darlastic/CaseBrowser/Flag",
                "GET /api/Darlastic/CaseBrowser/Audits",
                "GET /api/Darlastic/CaseBrowser/Case",
                "GET /api/Darlastic/CaseBrowser/Cases",
                "GET /api/Darlastic/CaseBrowser/Export",
                "GET /api/Darlastic/CaseBrowser/Flags",
                "GET /api/Darlastic/CaseBrowser/Identities",
                "GET /api/Darlastic/CaseBrowser/Identity/{identityId:long}",
                "GET /api/Darlastic/CaseBrowser/Search",
                "GET /api/Darlastic/CaseBrowser/Summary",
                "GET /api/Darlastic/CaseBrowserCompat/case",
                "GET /api/Darlastic/CaseBrowserCompat/cases",
                "GET /api/Darlastic/CaseBrowserCompat/cluster/{root:long}",
                "GET /api/Darlastic/CaseBrowserCompat/clusters",
                "GET /api/Darlastic/CaseBrowserCompat/flags",
                "GET /api/Darlastic/CaseBrowserCompat/record/{idx:int}",
                "GET /api/Darlastic/CaseBrowserCompat/search",
                "GET /api/Darlastic/CaseBrowserCompat/summary",
                "GET /api/Darlastic/CaseBrowserUi/token",
                "GET /api/Darlastic/GoldenCustomer/{id:long}",
                "GET /api/Darlastic/GoldenCustomer/{id:long}/sources",
                "POST /api/Darlastic/CaseBrowser/Audit",
                "POST /api/Darlastic/CaseBrowser/Flag",
                "POST /api/Darlastic/CaseBrowser/Flag/Respond",
                "POST /api/Darlastic/CaseBrowserCompat/audit",
                "POST /api/Darlastic/CaseBrowserCompat/flag",
                "POST /api/Darlastic/CaseBrowserCompat/unflag",
                "POST /api/Darlastic/StewardQueue/verdict",
            },

            Normalization = new NormalizerOptions
            {
                HeaderAllowlist = new(StringComparer.OrdinalIgnoreCase) { "Content-Language" },
                RunStart = DateTimeOffset.UtcNow,
            },
        };

        var summary = await ParityGroupRun.ExecuteAsync(
            client, factory.Services, groupConfig, grant, mode, parityRoot, ct);

        output.WriteLine("\n" + summary);
        output.WriteLine("\nSMOKE ONLY: 0 triples and 0 profiles in this group, so this run proves the");
        output.WriteLine("routes exist and respond. It proves NOTHING about mapping behaviour.");

        Assert.True(summary.Passes, "Parity gates failed:\n" + summary);
    }
}
