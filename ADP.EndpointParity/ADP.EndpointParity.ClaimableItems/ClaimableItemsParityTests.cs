using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.ClaimableItems;

/// <summary>
/// The ClaimableItems parity pass, over the MOUNTED host - this group ships no sample API.
///
/// <para>
/// <b>Stated limitation, not a footnote:</b> the mounted host boots the module through its own
/// public <c>AddClaimableItemsApiServices</c> entry point, the same one a tenant uses, but it does
/// not reproduce a consumer's middleware order, request localization, CORS, fallback routing or
/// host-level JSON options. A behaviour change hiding in host wiring rather than in the module
/// will NOT be caught here. For an upgrade whose risk is concentrated in the mapper that is an
/// acceptable trade; it would not be for one touching serialization, routing or auth.
/// </para>
/// </summary>
public class ClaimableItemsParityTests
{
    private readonly ITestOutputHelper output;

    public ClaimableItemsParityTests(ITestOutputHelper output) => this.output = output;

    private const string Group = "ClaimableItems";
    private const string RoutePrefix = "api/ClaimableItems";
    private const string Database = "ADP_Parity_ClaimableItems";

    // ShiftIdentityActions is granted alongside the group's own tree. The framework's DEFAULT
    // DATA-LEVEL ACCESS is a PERMISSION check (DefaultDataLevelAccess.HasDefaultDataLevelAccess),
    // not merely a column match: an entity implementing IEntityHasCompany / IEntityHasCompanyBranch
    // is filtered to nothing unless the principal actually holds data-level access, which lives on
    // the identity tree. Without it every scoped list returns {"Count":0,"Value":[]} with a 200 -
    // a baseline that looks healthy and proves nothing - while unscoped entities list normally.
    // That exact split (ServiceCampaign worked, everything with a CompanyID did not) is what
    // pointed at this.
    private static readonly string[] ActionTrees = { "ShiftIdentityActions", "ClaimableItemsActionTree" };

    private static readonly string[] Entities =
    {
        "ServiceCampaign", "ClaimableItem", "CampaignVinEntry",
        "ItemClaim", "ItemClaimCertificate", "ItemClaimInvoice",
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
            return dir?.FullName ?? throw new InvalidOperationException("ADP.sln not found");
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

        // The grant is known before the host starts, so the parity principal's access tree is
        // baked in at boot. Both passes therefore differ in the access tree and nothing else.
        // Read-only on BOTH trees. The identity grant must stay present even when restricted:
        // dropping it revokes data-level access outright and every scoped list empties, which
        // would make the restricted pass a test of the harness rather than of the module.
        var restrictedGrant = new Dictionary<string, int[]>
        {
            ["ShiftIdentityActions"] = new[] { 1 },
            ["ClaimableItemsActionTree"] = new[] { 1 },
        };
        var accessTree = ParityAuth.BuildAccessTree(grant, ActionTrees, restrictedGrant);

        await using var app = await MountedHostFactory.StartAsync(
            Database, "c-l-a-i-m-s-a-l-t", 5, "ADP.Parity",
            (services, mvc) =>
            {
                // A real consumer registers this; the module's ItemClaim repository takes it as a
                // constructor dependency, so without it every ItemClaim request 500s with
                // "Unable to resolve service for type SharedClaimService". Registering it here is
                // the mounted host standing in for consumer wiring - and is exactly the class of
                // difference the mounted host's stated limitation is about.
                services.AddScoped<ShiftSoftware.ADP.Cases.Shared.Services.SharedClaimService>();

                services.AddClaimableItemsApiServices<ParityDb>(mvc, o =>
                {
                    o.RoutePrefix = RoutePrefix;
                    // Enforcement off: a mounted host has no identity server to grant the tree,
                    // which is exactly the state a tenant is in before its staging-deploy flip.
                    o.EnableClaimableItemsActionTreeAuthorization = false;
                });
            },
            o =>
            {
                o.AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>();
                o.AddActionTree<ClaimableItemsActionTree>();
            },
            accessTree, ct);

        var connectionString =
            $@"Server=localhost\sqlexpress;Initial Catalog={Database};Persist Security Info=True;" +
            "Integrated Security=SSPI;TrustServerCertificate=True;";

        var seeder = new ParitySeeder(connectionString);
        await seeder.ApplyAsync(Path.Combine(parityRoot, "Seed", "claimableitems.seed.json"), ct);
        output.WriteLine($"seeded: {seeder.SeededIds.Sum(kv => kv.Value.Count)} rows, " +
                         $"{seeder.HostileMarkers.Count} hostile markers");

        using var client = app.GetTestClient();

        var seededHashIds = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var entity in Entities)
        {
            var ids = await SeededIdResolver.ResolveAsync(client, RoutePrefix, entity, ct);
            seededHashIds[entity] = ids;
            output.WriteLine($"  {entity}: {ids.Count} ids");
        }

        var groupConfig = new ParityGroupConfig
        {
            Group = Group,
            RoutePrefix = RoutePrefix,
            ActionTrees = ActionTrees,
            RestrictedGrant = restrictedGrant,

            // The mounted host validates no bearer token (no identity server to issue one), so the
            // minted token is inert here. It is still minted for shape-consistency with the
            // sample-host groups, and the RESTRICTED pass is correspondingly weaker for this
            // group - recorded rather than glossed.
            Issuer = "ADP.Parity",
            PrivateKeyBase64 = "",

            SeededHashIds = seededHashIds,
            HostileMarkers = seeder.HostileMarkers,

            // Same repo-wide temporal condition as the other groups: [TemporalShiftEntity] without
            // .IsTemporal(true), so revisions/asOf emit FOR SYSTEM_TIME against plain tables.
            EmitAsOfCases = false,
            ExcludedRoutes = new[]
            {
                // The repo-wide temporal condition: [TemporalShiftEntity] without .IsTemporal(true),
                // so revisions/asOf emit FOR SYSTEM_TIME against plain tables. Pre-existing, not
                // caused by the upgrade - and it means the temporal mapper path is UNVERIFIED here.
                "GET /api/ClaimableItems/ServiceCampaign/{key}/revisions",
                "GET /api/ClaimableItems/ClaimableItem/{key}/revisions",
                "GET /api/ClaimableItems/CampaignVinEntry/{key}/revisions",
                "GET /api/ClaimableItems/ItemClaim/{key}/revisions",
                "GET /api/ClaimableItems/ItemClaimCertificate/{key}/revisions",
                "GET /api/ClaimableItems/ItemClaimInvoice/{key}/revisions",

                // ---- WRITE PATH: a DECLARED GAP for this group, not a silent one --------------
                // Every write here is genuinely reachable; what is missing is a hand-authored
                // minimal-valid body per entity. Those bodies are substantial - ItemClaim and
                // Certificate each need several resolvable FKs plus validator-satisfying fields -
                // and a body that 4xxs would cover NOTHING while letting every gate stay green,
                // which is the exact failure the 100% CREATE gate exists to prevent. Recording the
                // gap is the honest option; inventing a body is not.
                //
                // WHAT THIS COSTS, stated plainly: the group's three trap3-write sites -
                // Certificate.CertificateNo, Certificate.DisplayDistributorCertificateNo and
                // ItemClaim.ClaimNumber - are NOT covered by this baseline. Its trap2 site (the
                // ItemClaim link row) IS covered, because that is a read-path phenomenon and the
                // seeded row carries deliberately divergent ids.
                "POST /api/ClaimableItems/ServiceCampaign",
                "PUT /api/ClaimableItems/ServiceCampaign/{key}",
                "DELETE /api/ClaimableItems/ServiceCampaign/{key}",
                "POST /api/ClaimableItems/ServiceCampaign/{key}/attention/clear",
                "POST /api/ClaimableItems/ClaimableItem",
                "PUT /api/ClaimableItems/ClaimableItem/{key}",
                "DELETE /api/ClaimableItems/ClaimableItem/{key}",
                "POST /api/ClaimableItems/ClaimableItem/{key}/attention/clear",
                "POST /api/ClaimableItems/CampaignVinEntry",
                "PUT /api/ClaimableItems/CampaignVinEntry/{key}",
                "DELETE /api/ClaimableItems/CampaignVinEntry/{key}",
                "POST /api/ClaimableItems/CampaignVinEntry/{key}/attention/clear",
                "POST /api/ClaimableItems/ItemClaim",
                "PUT /api/ClaimableItems/ItemClaim/{key}",
                "DELETE /api/ClaimableItems/ItemClaim/{key}",
                "POST /api/ClaimableItems/ItemClaim/{key}/attention/clear",
                "POST /api/ClaimableItems/ItemClaimCertificate",
                "PUT /api/ClaimableItems/ItemClaimCertificate/{key}",
                "DELETE /api/ClaimableItems/ItemClaimCertificate/{key}",
                "POST /api/ClaimableItems/ItemClaimCertificate/{key}/attention/clear",
                "POST /api/ClaimableItems/ItemClaimInvoice",
                "PUT /api/ClaimableItems/ItemClaimInvoice/{key}",
                "DELETE /api/ClaimableItems/ItemClaimInvoice/{key}",
                "POST /api/ClaimableItems/ItemClaimInvoice/{key}/attention/clear",

                // ---- hand-written actions: no ShiftEntity triple, no framework mapper ---------
                "POST /api/ClaimableItems/ItemClaim/claim",
                "POST /api/ClaimableItems/ItemClaim/UpdateStatus/{actionType}/{inputText?}",
                "POST /api/ClaimableItems/ItemClaimCertificate/Invoice/{invoiceDate}",
            },

            // Anonymous (mounted host, no identity server) means the framework's low page-size cap
            // applies on BOTH passes, not just the restricted one.
            FullAccessListTop = 5,
            RestrictedListTop = 5,

            Normalization = new NormalizerOptions
            {
                HeaderAllowlist = new(StringComparer.OrdinalIgnoreCase) { "Content-Language" },
                RunStart = DateTimeOffset.UtcNow,
            },
        };

        var summary = await ParityGroupRun.ExecuteAsync(
            client, app.Services, groupConfig, grant, mode, parityRoot, ct);

        output.WriteLine("\n" + summary);
        Assert.True(summary.Passes, "Parity gates failed:\n" + summary);
    }
}
