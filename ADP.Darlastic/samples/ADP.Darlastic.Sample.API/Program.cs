using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.CaseBrowser;
using ShiftSoftware.ADP.Darlastic.Sample.API;
using ShiftSoftware.ADP.Darlastic.Sample.API.Data;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

// ---------------------------------------------------------------------------------------------
// ADP.Darlastic sample host (CC8).
//
// What it is for: running the whole loop on one machine — a registry the spike's `resolve` filled,
// the module's API over it, and the case browser UI on top — so the surface is exercised before it
// is mounted in a tenant host. It is the harness that made the hosted controller testable without
// any tenant host being available.
//
// What it is NOT: a deployment. It authenticates nobody (see DevAuthenticationHandler) and reads
// real customer records straight out of whatever registry you point it at.
// ---------------------------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// The registry to serve. Point it at a tenant registry the engine has already resolved into
// (`DARLASTIC_DB=Darlastic-<tenant> dotnet run resolve` in the spike), or at a scratch one.
string? connection = builder.Configuration.GetConnectionString("Registry")
    ?? Environment.GetEnvironmentVariable("DARLASTIC_SAMPLE_SQL");

if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine(
        "No registry connection. Set ConnectionStrings:Registry in appsettings.Development.json, " +
        "or the DARLASTIC_SAMPLE_SQL environment variable.");
    return 1;
}

// ---- the bypass gate ------------------------------------------------------------------------
// Both conditions required. This sample serves real customer records; a misconfigured deployment
// must fail to start rather than serve them to anonymous callers.
bool devAuth = builder.Configuration.GetValue<bool>("Sample:AllowDevAuth");
if (devAuth && !builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine(
        "Sample:AllowDevAuth is set outside the Development environment. That combination " +
        "authenticates every caller as a developer against a real registry. Refusing to start.");
    return 1;
}
if (!devAuth)
{
    Console.Error.WriteLine(
        "This sample has no real authentication provider wired. Set Sample:AllowDevAuth=true in " +
        "Development to run it locally, or mount ADP.Darlastic.API in a host that has one " +
        "(that is the supported path — see the deployment architecture doc).");
    return 1;
}

builder.Services.AddDbContext<SampleDB>(o => o.UseSqlServer(connection));

var mvcBuilder = builder.Services.AddControllers();
mvcBuilder.AddShiftEntityWeb(x =>
{
    x.HashId.RegisterHashId(acceptUnencodedIds: true);
});

// ShiftEntity's data-level access resolves ITypeAuthService, so this must be registered even
// though the module's own action-tree enforcement is off below. AddDarlasticApiServices adds the
// Darlastic tree into the options it configures.
builder.Services.AddTypeAuth(_ => { });

builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
        DevAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddDarlasticApiServices<SampleDB>(mvcBuilder, options =>
{
    options.RoutePrefix = "api/Darlastic";
    // The action tree is not granted in this host (there is no identity server to grant it), so
    // enforcement stays off exactly as it does in a tenant before the staging-deploy flip.
    options.EnableDarlasticActionTreeAuthorization = false;
    // A fixed key, because this host is already a dev-only bypass — see DevAuthenticationHandler.
    // A tenant host reads this from its own secret configuration.
    options.CaseBrowserSigningKey = "sample-host-dev-signing-key-not-a-secret";
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// The case browser UI, served from the package's embedded copy. Same file the spike's local
// `queue` mode serves, so there is exactly one UI to keep working.
//
// Pointed at the COMPAT routes, not the module's own. Html() with no argument leaves the page on
// /api/*, which only the standalone server answers — the page then loads and 404s every one of its
// own data calls, which looks like a working deployment until someone opens it.
app.MapGet("/", () => Results.Content(
    CaseBrowserUI.Html("/api/Darlastic/CaseBrowserCompat"), "text/html; charset=utf-8"));

Console.WriteLine($"Darlastic sample host — registry: {new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog}");
Console.WriteLine($"  UI   http://localhost:5080");
Console.WriteLine($"  API  http://localhost:5080/api/Darlastic/CaseBrowser/Summary");
Console.WriteLine($"  UI API  http://localhost:5080/api/Darlastic/CaseBrowserCompat/summary");
Console.WriteLine("  auth: DEV BYPASS — every caller is authenticated as " + DevAuthenticationHandler.DevUser);

app.Run();
return 0;

// ------------------------------------------------------------------------------------------
// Makes the implicit top-level-statements Program class visible to the endpoint-parity harness
// so WebApplicationFactory<Program> can boot this host in-process. Adds no members, changes no
// behaviour, inert at run time.
//
// This host is the plan's ONLY framework-only control: 0 repository triples and 0 AutoMapper
// profiles, so a harness diff here is unambiguously caused by the framework rather than by a
// mapper rewrite. That is why booting it is worth this edit (SPIKE-5).
//
// KEPT DELIBERATELY at Step 08, which removed the endpoint-parity harness that originally
// needed it. Top-level statements generate an INTERNAL Program class, which
// WebApplicationFactory<T> in another assembly cannot name; this declaration only widens
// that visibility. It adds no members, changes no behaviour and is inert at run time, and it
// is the conventional shape for a sample host anyone may later want an integration test
// against - the next person writing one would simply have to add it back.
// ------------------------------------------------------------------------------------------
public partial class Program { }
