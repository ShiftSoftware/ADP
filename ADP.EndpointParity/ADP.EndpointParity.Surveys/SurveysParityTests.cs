using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.Surveys;

/// <summary>
/// The Surveys parity pass. Deliberately THIN — it supplies a host and a config; every piece of
/// logic lives in the harness library, which is what lets the five group projects be one csproj
/// each and lets a red group stay contained to its own project.
///
/// <para>Driven by <c>tools/parity.ps1</c> through <c>PARITY_MODE</c> / <c>PARITY_GRANT</c>.</para>
/// </summary>
public class SurveysParityTests
{
    private readonly ITestOutputHelper output;

    public SurveysParityTests(ITestOutputHelper output) => this.output = output;

    private const string Group = "Surveys";
    private const string RoutePrefix = "api/Surveys";
    private const string Database = "ADP_Parity_Surveys";

    private static readonly string[] ActionTrees =
    {
        "ShiftIdentityActions", "AzureStorageActionTree", "GeneralActionTree", "SurveysActionTree",
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

        var parityRoot = ParityGroupRun.ParityRootFromEnvironment(
            Path.Combine(RepoRoot, "ADP.EndpointParity"));
        var mode = ParityGroupRun.ModeFromEnvironment();
        var grant = ParityGroupRun.GrantFromEnvironment();

        output.WriteLine($"mode={mode} grant={grant} root={parityRoot}");

        // Every run gets its own fresh database. Rule 1's "same longs both runs" is only true if
        // nothing survives between runs - a contaminated database is the classic silent failure.
        await ParitySeeder.ResetDatabaseAsync(MasterConnection, Database, ct);

        await using var factory = new SampleHostFactory<Program>(
            connectionStringKey: "ConnectionStrings:SQLServer",
            databaseName: Database);

        var client = factory.CreateClient();
        var config = factory.Services.GetRequiredService<IConfiguration>();

        // ---- adversarial seed, explicit long PKs via IDENTITY_INSERT (SPIKE-3) -------------
        var seedPath = Path.Combine(parityRoot, "Seed", "surveys.seed.json");
        var connectionString = config.GetConnectionString("SQLServer")!;
        var seeder = new ParitySeeder(connectionString);
        await seeder.ApplyAsync(seedPath, ct);
        output.WriteLine($"seeded: {seeder.SeededIds.Sum(kv => kv.Value.Count)} rows, " +
                         $"{seeder.HostileMarkers.Count} hostile markers");

        // A token is needed before the list endpoints will answer, so mint the full-access one
        // just to resolve seeded hash ids; the real per-grant token is minted inside the run.
        var issuer = config["Settings:TokenSettings:Issuer"]!;
        var privateKey = config["Settings:TokenSettings:PrivateKey"]!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", ParityAuth.MintToken(
            issuer, privateKey,
            ParityAuth.BuildAccessTree(ParityGrant.FullAccess, ActionTrees, new Dictionary<string, int[]>())));

        var entities = new[] { "Survey", "SurveyInstance", "BankQuestion", "ScreenTemplate" };
        var seededHashIds = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var entity in entities)
        {
            var ids = await SeededIdResolver.ResolveAsync(client, RoutePrefix, entity, ct);
            seededHashIds[entity] = ids;
            output.WriteLine($"  {entity}: {ids.Count} seeded ids -> {string.Join(", ", ids)}");
        }

        // Hand-authored minimal-valid bodies. Loaded from disk so they are reviewable as data
        // rather than buried in C# string literals.
        var createBodies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entity in new[] { "Survey", "BankQuestion", "ScreenTemplate" })
        {
            var path = Path.Combine(parityRoot, "Seed", $"surveys.{entity}.create.json");
            if (File.Exists(path)) createBodies[entity] = await File.ReadAllTextAsync(path, ct);
        }
        output.WriteLine($"create bodies loaded: {string.Join(", ", createBodies.Keys)}");

        // UPDATE reuses the same hand-authored body. Same DTO, same validator, and the same
        // read-only sentinels (publishedVersionNumber=4242, locked=true) - which is the point:
        // an ignored member must stay ignored on the UPDATE path too, and PUT is the likelier
        // place for a convention mapper to start writing it.
        var updateBodies = new Dictionary<string, string>(createBodies, StringComparer.Ordinal);

        var groupConfig = new ParityGroupConfig
        {
            Group = Group,
            RoutePrefix = RoutePrefix,
            ActionTrees = ActionTrees,
            RestrictedGrant = new Dictionary<string, int[]> { ["SurveysActionTree"] = new[] { 1 } },
            Issuer = issuer,
            PrivateKeyBase64 = privateKey,
            SeededHashIds = seededHashIds,
            HostileMarkers = seeder.HostileMarkers,
            CreateBodies = createBodies,
            UpdateBodies = updateBodies,

            // The Surveys tables are NOT system-versioned - entities carry [TemporalShiftEntity]
            // but nothing calls .IsTemporal(true), so EF reports IsTemporal=False while the
            // inherited asOf route still emits FOR SYSTEM_TIME SQL and 500s. Pre-existing, and
            // unrelated to the upgrade. Emitting the case would bank a 500 into the baseline.
            EmitAsOfCases = false,

            // Pre-existing 500s, not upgrade regressions: Survey entities carry
            // [TemporalShiftEntity] but nothing calls .IsTemporal(true), so EF reports
            // IsTemporal=False while the inherited routes still emit FOR SYSTEM_TIME SQL.
            // Recorded here rather than silently tolerated - the temporal mapper path is
            // UNVERIFIED for this group, and a green run must not imply otherwise.
            ExcludedRoutes = new[]
            {
                // --- pre-existing 500s: not system-versioned (see EmitAsOfCases above) --------
                "GET /api/Surveys/Survey/{key}/revisions",
                "GET /api/Surveys/SurveyInstance/{key}/revisions",
                "GET /api/Surveys/BankQuestion/{key}/revisions",
                "GET /api/Surveys/ScreenTemplate/{key}/revisions",

                // --- SurveyInstance: every write verb is overridden to 405 -------------------
                // Capturing a 405 proves nothing about the mapper, and SurveyInstance is already
                // listed in WriteUnreachable, which is the honest record of that gap.
                "POST /api/Surveys/SurveyInstance",
                "PUT /api/Surveys/SurveyInstance/{key}",
                // DELETE too: with no reachable CREATE there is no row of our own to delete, and
                // deleting a SEEDED row would change every later list body and make the run
                // order-dependent.
                "DELETE /api/Surveys/SurveyInstance/{key}",

                // --- attention-signal clear: MUTATES state mid-run ---------------------------
                // POST {entity}/{key}/attention/clear clears signals for the row it names, which
                // would change the ATTENTION body of a later case and make the run order-dependent.
                // The read side ({key}/attention) IS covered.
                "POST /api/Surveys/Survey/{key}/attention/clear",
                "POST /api/Surveys/BankQuestion/{key}/attention/clear",
                "POST /api/Surveys/ScreenTemplate/{key}/attention/clear",
                "POST /api/Surveys/SurveyInstance/{key}/attention/clear",

                // --- hand-written controllers: no ShiftEntity triple, no framework mapper -----
                // Preview, Publish, PublicSurvey, SurveyResponses and the Trigger controllers are
                // plain ControllerBase with hand-written bodies. A MAPPER upgrade cannot silently
                // change them, which is why they are out of this step's claim. They are still real
                // endpoints: if this migration ever touches serialization or routing rather than
                // mapping, they must be covered before that claim is made.
                "POST /api/Surveys/Preview",
                "POST /api/Surveys/Publish/{id}",
                "POST /api/Surveys/SurveyInstances/{publicId:guid}/responses",
                "GET /api/Surveys/SurveyInstances/{publicId:guid}/schema",
                "GET /api/Surveys/SurveyInstances/{publicId:guid}/status",
                "GET /api/Surveys/SurveyResponses/instance/{publicId:guid}",
                "GET /api/Surveys/SurveyResponses/public-url-template",
                "GET /api/Surveys/SurveyResponses/{surveyId}/export",
                "POST /api/Surveys/SurveyResponses/{surveyId}/test-instances",
                "GET /api/Surveys/Triggers/channels",
                "POST /api/Surveys/Triggers/ingest",
                "POST /api/Surveys/Triggers/scheduler/tick",
            },

            // SurveyInstanceController overrides every write verb to 405, yet
            // SurveyInstanceRepository is a live triple driven from the public submit and
            // trigger-ingest paths. It needs a mapper-level write golden instead.
            WriteUnreachable = new[] { "SurveyInstance" },

            Normalization = new NormalizerOptions
            {
                HeaderAllowlist = new(StringComparer.OrdinalIgnoreCase) { "Content-Language" },
                RunStart = DateTimeOffset.UtcNow,

                // BankQuestionRepository mints BankEntryID with Guid.NewGuid() on create, so it
                // differs between two otherwise-identical runs. The stability gate caught this;
                // it is not anticipated drift. Seeded guids stay LITERAL (see below), so a wrong
                // BankEntryID on a seeded row is still a diff - only the freshly-minted one on
                // the row this run creates is tokenised.
                ServerGeneratedGuidNames = new(StringComparer.Ordinal) { "BankEntryID" },
                KnownDeterministicValues = new(StringComparer.OrdinalIgnoreCase)
                {
                    "b0000000-0000-4000-8000-000000000001",
                    "b0000000-0000-4000-8000-000000000002",
                    "a0000000-0000-4000-8000-000000000001",
                },
            },
        };

        var summary = await ParityGroupRun.ExecuteAsync(
            client, factory.Services, groupConfig, grant, mode, parityRoot, ct);

        output.WriteLine("\n" + summary);

        Assert.True(summary.Passes, "Parity gates failed:\n" + summary);
    }
}
