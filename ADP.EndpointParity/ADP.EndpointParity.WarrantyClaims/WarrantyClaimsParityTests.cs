using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.WarrantyClaims;

/// <summary>
/// The WarrantyClaims parity pass, over the MOUNTED host - this group ships no sample API.
///
/// <para>
/// <b>Stated limitation, not a footnote:</b> the mounted host boots the module through its own
/// public <c>AddWarrantyClaimsApiServices</c> entry point, the same one a tenant uses, but it does
/// not reproduce a consumer's middleware order, request localization, CORS, fallback routing or
/// host-level JSON options. A behaviour change hiding in host wiring rather than in the module
/// will NOT be caught here. For an upgrade whose risk is concentrated in the mapper that is an
/// acceptable trade; it would not be for one touching serialization, routing or auth.
/// </para>
/// </summary>
/// <summary>
/// A DEALER-side capability provider. See the registration comment for why this specific value is
/// the one the trap 3-read baseline must be captured under.
/// </summary>
internal sealed class ParityDealerCapabilityProvider
    : ShiftSoftware.ADP.WarrantyClaims.Shared.IWarrantyClaimsCapabilityProvider
{
    public bool IsDistributor => false;
}

public class WarrantyClaimsParityTests
{
    private readonly ITestOutputHelper output;

    public WarrantyClaimsParityTests(ITestOutputHelper output) => this.output = output;

    private const string Group = "WarrantyClaims";
    private const string RoutePrefix = "api/WarrantyClaims";
    private const string Database = "ADP_Parity_WarrantyClaims";

    // This group declares NO action tree of its own: every gating action is consumer-supplied
    // (WarrantyClaimsApiOptions.cs:22-103, all nullable, all left null here). That is not an
    // omission - it is exactly what makes the DealerFinancial exposure visible on the ORDINARY
    // full-access pass, because the controller's gate does not run at all when its action is null.
    // ShiftIdentityActions is included DELIBERATELY even though this group declares no tree of
    // its own. The framework's DEFAULT DATA-LEVEL ACCESS is a permission check
    // (DefaultDataLevelAccess.HasDefaultDataLevelAccess), not merely a column match: an entity
    // implementing IEntityHasCompany / IEntityHasCompanyBranch is filtered to nothing unless the
    // principal is actually granted data-level access, which lives on the identity tree. Without
    // it every scoped list returns {"Count":0,"Value":[]} with a 200 while unscoped entities
    // (AdditionalLaborOperationCode, ManufacturerSettlmentSheet) list normally - which is exactly
    // the split the first captures showed.
    private static readonly string[] ActionTrees = { "ShiftIdentityActions" };

    private static readonly string[] Entities =
    {
        "WarrantyClaim", "WarrantyCertificate", "WarrantyInvoice",
        "DealerFinancial", "DistributorFinancial",
        "ManufacturerSettlmentSheet", "AdditionalLaborOperationCode", "WarrantyRates",
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
        // Restricted = READ-only on the identity tree. Not an empty tree: an empty one revokes
        // data-level access entirely, so every scoped list returns nothing and the pass degenerates
        // into "the principal can see no data", which tests the harness rather than the module.
        // Read-only keeps the principal able to SEE rows while unable to change them, which is the
        // boundary worth capturing.
        var restrictedGrant = new Dictionary<string, int[]> { ["ShiftIdentityActions"] = new[] { 1 } };
        var accessTree = ParityAuth.BuildAccessTree(grant, ActionTrees, restrictedGrant);

        await using var app = await MountedHostFactory.StartAsync(
            Database, "w-a-r-r-a-n-t-y-s-a-l-t", 5, "ADP.Parity",
            (services, mvc) =>
            {
                // ---- consumer wiring the module does not register for itself -----------------
                // Each of these is a constructor dependency of a repository or controller in this
                // group, and each was found by a capture coming back 500 "Unable to resolve
                // service for type ...". A real host registers them; the mounted host has to
                // stand in, and this list is the most concrete evidence in the plan for why that
                // mode is "one notch below" a sample host.
                services.AddScoped<ShiftSoftware.ADP.WarrantyClaims.Data.Services.WarrantyClaimService>();
                services.AddScoped<ShiftSoftware.ADP.Cases.Shared.Services.SharedClaimService>();
                services.AddScoped<ShiftSoftware.ADP.WarrantyClaims.Data.Services.DeliveryDateService>();
                services.AddHttpClient();

                // THE DEALER / DISTRIBUTOR SWITCH, and the reason this group is the highest-risk
                // one in the plan. IWarrantyClaimsCapabilityProvider.IsDistributor drives the
                // "DTO distributor-field stripping in ViewAsync" its own doc comment describes.
                // Capturing as a DEALER (false) is precisely the configuration in which the five
                // distributor-side members MUST come back null - which is what makes a
                // post-upgrade diff on them mean "the convention mapper started populating fields
                // the old profile deliberately blanked".
                services.AddScoped<ShiftSoftware.ADP.WarrantyClaims.Shared.IWarrantyClaimsCapabilityProvider,
                                   ParityDealerCapabilityProvider>();

                services.AddWarrantyClaimsApiServices<ParityDb>(mvc, o =>
                {
                    o.RoutePrefix = RoutePrefix;
                    // Enforcement off: a mounted host has no identity server to grant the tree,
                    // which is exactly the state a tenant is in before its staging-deploy flip.
                    o.EnableWarrantyClaimsActionTreeAuthorization = false;
                });
            },
            o => o.AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>(),
            accessTree, ct);

        var connectionString =
            $@"Server=localhost\sqlexpress;Initial Catalog={Database};Persist Security Info=True;" +
            "Integrated Security=SSPI;TrustServerCertificate=True;";

        var seeder = new ParitySeeder(connectionString);
        await seeder.ApplyAsync(Path.Combine(parityRoot, "Seed", "warrantyclaims.seed.json"), ct);
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

            // ---- EXCLUDED, each with a reason. Four categories, all real: -------------------
            //
            // 1. TEMPORAL ({key}/revisions): [TemporalShiftEntity] entities where nothing calls
            //    .IsTemporal(true), so the route emits FOR SYSTEM_TIME against a plain table and
            //    500s. Repo-wide, pre-existing, unrelated to the upgrade. The temporal mapper path
            //    is therefore UNVERIFIED for this group.
            //
            // 2. attention/clear: MUTATES state mid-run, which would change a later ATTENTION
            //    body and make the run order-dependent. The READ side ({key}/attention) is covered.
            //
            // 3. WRITE PATH (POST/PUT/DELETE): a DECLARED gap. These writes are reachable; what is
            //    missing is a hand-authored minimal-valid body per entity. A body that 4xxs would
            //    cover nothing while letting every gate stay green - the exact failure the 100%
            //    CREATE gate exists to prevent - so recording the gap is the honest option.
            //    COST: this group's trap3-write sites on the shared Certificate entity are not
            //    covered. Its trap3-READ site - the five distributor members on DealerFinancial,
            //    the highest-risk item in the whole migration - IS covered, because that is a
            //    read-path phenomenon and the seeded claim carries all five non-null.
            //
            // 4. Reads with no seeded row (WarrantyCertificate / WarrantyInvoice / WarrantyRates /
            //    DistributorFinancial item routes) and hand-written actions with no ShiftEntity
            //    triple (exports, lookups, GenerateFromClaims). DistributorFinancial additionally
            //    answers 401 to this DEALER principal, which is correct behaviour, not a gap.
            ExcludedRoutes = new[]
            {
                "GET /api/WarrantyClaims/DistributorFinancial/Export",
                "GET /api/WarrantyClaims/DistributorFinancial/print-token/{key}",
                "GET /api/WarrantyClaims/DistributorFinancial/print/{key}",
                "GET /api/WarrantyClaims/DistributorFinancial/{key}",
                "GET /api/WarrantyClaims/DistributorFinancial/{key}/attention",
                "GET /api/WarrantyClaims/DistributorFinancial/{key}/revisions",
                "GET /api/WarrantyClaims/ManufacturerSettlmentSheet/GenerateFromClaims",
                "GET /api/WarrantyClaims/WarrantyCertificate/print-token/{key}",
                "GET /api/WarrantyClaims/WarrantyCertificate/print/{key}",
                "GET /api/WarrantyClaims/WarrantyCertificate/{key}",
                "GET /api/WarrantyClaims/WarrantyCertificate/{key}/attention",
                "GET /api/WarrantyClaims/WarrantyCertificate/{key}/revisions",
                "GET /api/WarrantyClaims/WarrantyClaim/DownloadCSV/{*exportPath}",
                "GET /api/WarrantyClaims/WarrantyClaim/current-rates",
                "GET /api/WarrantyClaims/WarrantyClaim/flat-rate/{vds}/{wmi?}/{brandHashId}",
                "GET /api/WarrantyClaims/WarrantyClaim/part-lookup/{partNumber}",
                "GET /api/WarrantyClaims/WarrantyClaim/print-invoice-token/{key}",
                "GET /api/WarrantyClaims/WarrantyClaim/print-invoice/{key}",
                "GET /api/WarrantyClaims/WarrantyClaim/vin-lookup/{vin}",
                "GET /api/WarrantyClaims/WarrantyInvoice/print-token/{key}",
                "GET /api/WarrantyClaims/WarrantyInvoice/print/{key}",
                "GET /api/WarrantyClaims/WarrantyInvoice/{key}",
                "GET /api/WarrantyClaims/WarrantyInvoice/{key}/attention",
                "GET /api/WarrantyClaims/WarrantyInvoice/{key}/revisions",
                "GET /api/WarrantyClaims/WarrantyRates/print-token/{key}",
                "GET /api/WarrantyClaims/WarrantyRates/print/{key}",
                "GET /api/WarrantyClaims/WarrantyRates/{key}",
                "GET /api/WarrantyClaims/WarrantyRates/{key}/attention",
                "GET /api/WarrantyClaims/WarrantyRates/{key}/revisions",
                "GET /api/WarrantyClaims/WarrantyClaim/{key}/revisions",
                "GET /api/WarrantyClaims/DealerFinancial/{key}/revisions",
                "GET /api/WarrantyClaims/ManufacturerSettlmentSheet/{key}/revisions",
                "GET /api/WarrantyClaims/AdditionalLaborOperationCode/{key}/revisions",

                // DELETE goes with POST/PUT: the lifecycle deletes the row this run
                // CREATED, and there is none. Deleting a SEEDED row instead would change
                // every later list body and make the run order-dependent.
                "DELETE /api/WarrantyClaims/WarrantyClaim/{key}",
                "DELETE /api/WarrantyClaims/WarrantyCertificate/{key}",
                "DELETE /api/WarrantyClaims/WarrantyInvoice/{key}",
                "DELETE /api/WarrantyClaims/DealerFinancial/{key}",
                "DELETE /api/WarrantyClaims/DistributorFinancial/{key}",
                "DELETE /api/WarrantyClaims/ManufacturerSettlmentSheet/{key}",
                "DELETE /api/WarrantyClaims/AdditionalLaborOperationCode/{key}",
                "DELETE /api/WarrantyClaims/WarrantyRates/{key}",
                "POST /api/WarrantyClaims/AdditionalLaborOperationCode",
                "POST /api/WarrantyClaims/AdditionalLaborOperationCode/{key}/attention/clear",
                "POST /api/WarrantyClaims/DealerFinancial",
                "POST /api/WarrantyClaims/DealerFinancial/{key}/attention/clear",
                "POST /api/WarrantyClaims/DistributorFinancial",
                "POST /api/WarrantyClaims/DistributorFinancial/Export",
                "POST /api/WarrantyClaims/DistributorFinancial/{key}/attention/clear",
                "POST /api/WarrantyClaims/ManufacturerSettlmentSheet",
                "POST /api/WarrantyClaims/ManufacturerSettlmentSheet/{key}/attention/clear",
                "POST /api/WarrantyClaims/WarrantyCertificate",
                "POST /api/WarrantyClaims/WarrantyCertificate/Invoice/{invoiceDate}",
                "POST /api/WarrantyClaims/WarrantyCertificate/{key}/attention/clear",
                "POST /api/WarrantyClaims/WarrantyClaim",
                "POST /api/WarrantyClaims/WarrantyClaim/ExportManufacturerCSV",
                "POST /api/WarrantyClaims/WarrantyClaim/UpdateDeliveryDate/{deliveryDate}",
                "POST /api/WarrantyClaims/WarrantyClaim/UpdateStatus/{actionType}/{inputText?}",
                "POST /api/WarrantyClaims/WarrantyClaim/{key}/attention/clear",
                "POST /api/WarrantyClaims/WarrantyInvoice",
                "POST /api/WarrantyClaims/WarrantyInvoice/{key}/attention/clear",
                "POST /api/WarrantyClaims/WarrantyRates",
                "POST /api/WarrantyClaims/WarrantyRates/{key}/attention/clear",
                "PUT /api/WarrantyClaims/AdditionalLaborOperationCode/{key}",
                "PUT /api/WarrantyClaims/DealerFinancial/{key}",
                "PUT /api/WarrantyClaims/DistributorFinancial/{key}",
                "PUT /api/WarrantyClaims/ManufacturerSettlmentSheet/{key}",
                "PUT /api/WarrantyClaims/WarrantyCertificate/{key}",
                "PUT /api/WarrantyClaims/WarrantyClaim/{key}",
                "PUT /api/WarrantyClaims/WarrantyInvoice/{key}",
                "PUT /api/WarrantyClaims/WarrantyRates/{key}",
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
