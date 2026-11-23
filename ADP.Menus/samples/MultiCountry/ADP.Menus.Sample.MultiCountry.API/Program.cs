using Microsoft.AspNetCore.Localization;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Extensions;
using ShiftSoftware.ADP.Menus.Sync.Extensions;
using ShiftSoftware.ADP.Menus.Sync.Replication;
using ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Data;
using ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.DataServices;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Identity is EXTERNAL: users sign in on the multi-country sample identity server (ADP.Identity), and this API
// only validates its tokens. Its issuer, public key and hash-id salt are in appsettings.Development.json.
var identity = builder.Configuration.GetSection("ShiftIdentity");

// Checked after Build() rather than here, so `dotnet ef` (which stops at Build) works without them.
string[] requiredSettings = ["ShiftIdentity:Issuer", "ShiftIdentity:PublicKey"];
var missingSettings = requiredSettings.Where(x => string.IsNullOrWhiteSpace(builder.Configuration[x])).ToList();

builder.Services.AddDbContext<DB>(db =>
    db.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")));

// ---------- Cosmos / Menu interface implementations ----------
// Two Cosmos accounts, configured separately, because they need not be the same one:
//   ConnectionStrings:PartPriceCosmos   — where CosmosService READS part prices (CompanyData/Parts). Pointing
//                                         it at a real account (user secrets) gives real prices while the
//                                         menus stay local.
//   ConnectionStrings:ReplicationCosmos — where the menu replication WRITES (the Services database). Gates
//                                         the replication further down.
// appsettings.Development.json points both at the emulator. Each client is keyed by its role, so nothing
// can pick up the wrong one by type alone.
var partPriceCosmosConnectionString = builder.Configuration.GetConnectionString(CosmosAccounts.PartPrice);
var replicationCosmosConnectionString = builder.Configuration.GetConnectionString(CosmosAccounts.Replication);
var replicationIsConfigured = !string.IsNullOrWhiteSpace(replicationCosmosConnectionString);

if (!string.IsNullOrWhiteSpace(partPriceCosmosConnectionString))
    builder.Services.AddKeyedSingleton(CosmosAccounts.PartPrice, new CosmosClient(partPriceCosmosConnectionString));

if (replicationIsConfigured)
    builder.Services.AddKeyedSingleton(CosmosAccounts.Replication, new CosmosClient(replicationCosmosConnectionString));

builder.Services.AddScoped<IMenuPartPriceService, CosmosService>();
builder.Services.AddScoped<IMenuReportExporter, MenuReportExporter>();
builder.Services.AddSingleton<IMenuCountryProvider, CountryProvider>();

// ---------- Generic ASP.NET infrastructure ----------
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

// ---------- ShiftEntity Print ----------
builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = "One-Two-Three";
});

// ---------- Controllers (consumer owns this) ----------
var mvcBuilder = builder.Services.AddControllers();

// ---------- ShiftEntity Web (HashId, AzureStorage, data assemblies) ----------
var azureStorageAccounts = new List<AzureStorageOption>();
builder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Data.DB).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);

    x.HashId.RegisterHashId(builder.Configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    // Must match the identity server, or company, branch and country IDs will not decode.
    x.HashId.RegisterIdentityHashId(identity["HashIds:Salt"] ?? "", identity.GetValue<int>("HashIds:MinHashLength"));

    x.AddAzureStorage(azureStorageAccounts.ToArray());
});

// ---------- External identity: validate the identity server's tokens ----------
if (missingSettings.Count == 0)
    mvcBuilder.AddShiftIdentity(identity["Issuer"]!, identity["PublicKey"]!);

// ---------- TypeAuth (consumer owns this; adds Menu action tree when enabled) ----------
builder.Services.AddTypeAuth(o =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<AzureStorageActionTree>();
    o.AddActionTree<GeneralActionTree>();
});

// ---------- Menu API services ----------
builder.Services.AddMenuApiServices<DB>(mvcBuilder, options =>
{
    options.RoutePrefix = "api/Menu";
    // The sample identity server registers the menu action tree, so its access trees grant it.
    options.EnableMenuActionTreeAuthorization = true;
    options.Languages = new()
    {
        new MenuLanguageOption("en-US", "English"),
        new MenuLanguageOption("ru-RU", "Russian"),
        new MenuLanguageOption("uz-UZ", "Uzbek"),
        new MenuLanguageOption("tg-TJ", "Tajik"),
        new MenuLanguageOption("tk-TM", "Turkmen"),
    };
});

// ---------- Menu → Cosmos replication (opt-in) ----------
// Projects the menu catalog into Cosmos on every SaveChanges: the variant graph into ServiceMenus
// (partitioned by basic model code, fully denormalized) and each master entity into its own
// container. A consumer that never calls AddMenuReplications replicates nothing — which is what a
// read-only host, fed by its own sync agent, wants. See COSMOS_REPLICATION_PLAN.md §16/§17.
//
// Replication is fire-and-forget: failures are logged and never surface to the save that triggered
// them, so watch the log rather than the HTTP response when checking whether it worked.
//
// This call also enables EFCore.Triggered on DB — the after-save hook replication rides on — so the
// AddDbContext above does not need to. Both option actions apply, whichever order they run in.
if (replicationIsConfigured)
{
    builder.Services.AddShiftEntityCosmosDbReplicationTrigger<DB>(x =>
        x.AddMenuReplications<DB>(x.Services.GetRequiredKeyedService<CosmosClient>(CosmosAccounts.Replication)));
}

// ---------- Consumer Todo repository ----------
builder.Services.AddScoped<ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Data.Repositories.TodoItemRepository>();

// ---------- ShiftMapper ----------
// Registers THIS assembly's generated mapper, which holds the Todo repository's maps. The menu maps need no line
// here — AddMenuApiServices registers its own assembly's — but this one carries them too, re-generated here, and
// answers first.
builder.Services.AddShiftMapper();

// ---------- Development-only data import ----------
// Sample scaffolding for loading a realistic dataset into the local sample database. The endpoints
// refuse to run outside Development; the source comes from ConnectionStrings:MenuImportSource. Not part of
// any ADP.Menus package.
builder.Services.AddScoped<ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.DevTools.SchemaCopyService>();

builder.Services.AddRazorPages();

var app = builder.Build();

var supportedCultures = new List<CultureInfo>
{
    new CultureInfo("en-US"),
    new CultureInfo("ru-RU"),
    new CultureInfo("uz-UZ"),
    new CultureInfo("tg-TJ"),
    new CultureInfo("tk-TM"),
};

app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider> { new AcceptLanguageHeaderRequestCultureProvider() };
    options.ApplyCurrentCultureToResponseHeaders = true;
});

app.MapControllers();

// Attribute-driven CRUD for [ShiftEntityEndpoint<>] / [ShiftEntitySecureEndpoint<>] entities. The Menus
// module's own controllers are classic ShiftEntitySecureControllerAsync and stay on MapControllers() above.
app.MapShiftEntityEndpoints<DB>();

if (missingSettings.Count > 0)
    throw new InvalidOperationException(
        $"Set these in the API's appsettings.Development.json before running the sample: {string.Join(", ", missingSettings)}.");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();

    // A database the sample used to make with EnsureCreated has the tables but not the migration history, so the
    // first migration would fail on tables that already exist.
    if (await db.Database.CanConnectAsync()
        && !(await db.Database.GetAppliedMigrationsAsync()).Contains(db.Database.GetMigrations().First())
        && await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE SCHEMA_NAME(schema_id) = 'Menu'").SingleAsync() > 0)
        throw new InvalidOperationException(
            $"The database '{db.Database.GetDbConnection().Database}' was created without migrations. Drop it and start the "
            + "API again to have it created by migrations, or add the migrations its schema already has to [__EFMigrationsHistory].");

    await db.Database.MigrateAsync();

    // Nothing is seeded here any more, deliberately.
    //
    // Seeding at startup put demo rows into the database silently, and the replication trigger then
    // copied them into Cosmos. Removing them again is the problem: the dev data import empties the
    // [Menu] schema in raw SQL, so no trigger fires and the documents are left behind — a basic model
    // code the lookup keeps serving that is nowhere in the database. Startup was also the wrong moment
    // for it, since it ran against whatever catalogue happened to be loaded.
    //
    // The same data now lives in ADP.Menus.Tests as SampleDataSeedingTests, which you trigger yourself
    // and which inserts only the rows that are missing:
    //
    //     dotnet test ADP.Menus/ADP.Menus.Tests --filter SampleDataSeeding
}

// RU/s the Services database is created with. Kept in step with ServiceMenusProvisioningTests, which
// provisions the same way — a developer's emulator and this sample should not differ.
//
// Manual, and therefore billed flat: fine against the emulator this sample points at, and a real cost
// on a real account. A host copying this should decide its own number (and consider autoscale, where
// CreateAutoscaleThroughput reads the value as the MAXIMUM rather than a floor).
const int MenuDatabaseThroughput = 25000;

// Create the containers the menu replication writes to. Without them every replication attempt fails,
// and because replication is fire-and-forget that failure is only a log line — the sample would look
// wired up and quietly write nothing.
//
// A real host provisions through its deployment pipeline; the sample does it at startup so a clean
// checkout plus a running emulator is a working setup. Best effort by design: appsettings.Development
// points at the emulator unconditionally, so a developer who does not have one running must still be
// able to boot the sample.
if (replicationIsConfigured)
{
    var cosmosLogger = app.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        using var provisioning = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // One call creates the database and all seven containers and verifies each partition key.
        // MenuCosmosContainers.All is the single declaration of what to create and with which key —
        // a key cannot be changed after creation, so it is never retyped at a call site.
        //
        // Shared database throughput, so all seven containers draw from one pool rather than each
        // needing its own. The backfill is what sizes it: replicate-all writes the whole catalogue
        // concurrently with bulk execution on, and a small pool makes it fail rows silently and need
        // several runs to converge (COSMOS_REPLICATION_PLAN.md §18 recorded 1060 of 1188 written).
        //
        // Applied ONLY when this call CREATES the database. CreateDatabaseIfNotExistsAsync ignores it
        // for one that already exists, so raising it here does not re-scale an existing database —
        // change that one on the account.
        var report = await MenuCosmosProvisioning.EnsureContainersAsync(
            app.Services.GetRequiredKeyedService<CosmosClient>(CosmosAccounts.Replication),
            databaseThroughput: ThroughputProperties.CreateManualThroughput(MenuDatabaseThroughput),
            cancellationToken: provisioning.Token);

        cosmosLogger.LogInformation(
            "Menu replication: {ContainerCount} containers ready in the {Database} database ({CreatedCount} created now).",
            report.Containers.Count,
            report.DatabaseName,
            report.CreatedCount);
    }
    catch (InvalidOperationException exception)
    {
        // A container that exists with the WRONG partition key. Not a missing emulator and not
        // fixable by restarting — it has to be dropped and recreated — so it is logged as an error.
        cosmosLogger.LogError(
            exception,
            "Menu replication: the Cosmos containers are provisioned with the wrong partition key.");
    }
    catch (Exception exception)
    {
        cosmosLogger.LogWarning(
            exception,
            "Menu replication: could not provision the Cosmos containers. The sample will run, but menu "
            + "changes will not reach Cosmos. Start the emulator (or point "
            + "ConnectionStrings:ReplicationCosmos elsewhere) and restart.");
    }
}

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/_framework")
            || context.Request.Path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path == "/")
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                return Task.CompletedTask;
            });
        }

        await next();
    });
}

// Aspire runs the Blazor app as its own resource and turns this off; run on its own, the API serves it.
var hostBlazorClient = app.Configuration.GetValue("Sample:HostBlazorClient", true);
if (hostBlazorClient)
    app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
if (hostBlazorClient)
    app.MapFallbackToFile("index.html");

app.Run();

// ------------------------------------------------------------------------------------------
// Makes the implicit top-level-statements Program class visible to the endpoint-parity harness
// so WebApplicationFactory<Program> can boot this host in-process. Adds no members, changes no
// behaviour, inert at run time.
//
// Note this sample needs NO seeding-suppression branch, unlike the Surveys one: it seeds nothing
// at startup (identity lives on the external identity server, and demo menu seeding was
// deliberately removed), so the adversarial seed is the only source of rows there.
//
// KEPT DELIBERATELY at Step 08, which removed the endpoint-parity harness that originally
// needed it. Top-level statements generate an INTERNAL Program class, which
// WebApplicationFactory<T> in another assembly cannot name; this declaration only widens
// that visibility. It adds no members, changes no behaviour and is inert at run time, and it
// is the conventional shape for a sample host anyone may later want an integration test
// against - the next person writing one would simply have to add it back.
// ------------------------------------------------------------------------------------------
public partial class Program { }
