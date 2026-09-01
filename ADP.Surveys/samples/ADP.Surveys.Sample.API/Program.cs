using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Sample.API.Data;
using ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;
using ShiftSoftware.ADP.Surveys.Sample.API.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Models;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- DbContext ----------
builder.Services.AddDbContext<DB>(db =>
    db.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")));

// ---------- Generic ASP.NET infrastructure ----------
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

// ---------- ShiftEntity Print (required by ShiftEntity.Web internals) ----------
builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = "One-Two-Three";
});

// ---------- Controllers (consumer owns this, then threads MvcBuilder through) ----------
var mvcBuilder = builder.Services.AddControllers();

// ---------- ShiftEntity Web (HashId, AutoMapper, data assemblies) ----------
mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(DB).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);
    x.AddAutoMapper(typeof(DB).Assembly);

    x.HashId.RegisterHashId(builder.Configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    x.HashId.RegisterIdentityHashId(
        builder.Configuration.GetValue<string>("Settings:IdentityHashIdSettings:Salt") ?? "",
        builder.Configuration.GetValue<int>("Settings:IdentityHashIdSettings:MinHashLength")
    );

    x.AddShiftIdentityAutoMapper();
});

// ---------- ShiftIdentity token validation ----------
mvcBuilder.AddShiftIdentity(
    builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer") ?? "",
    builder.Configuration.GetValue<string>("Settings:TokenSettings:PublicKey") ?? ""
);

// ---------- ShiftIdentity Dashboard (internal hosting: identity lives inside this API) ----------
builder.Services.AddScoped<ISendEmailVerification, SendEmailService>();
builder.Services.AddScoped<ISendEmailResetPassword, SendEmailService>();

mvcBuilder.AddShiftIdentityDashboard<DB>(
    new ShiftIdentityConfiguration
    {
        ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal,
        Token = new TokenSettingsModel
        {
            ExpireSeconds = builder.Configuration.GetValue<int>("Settings:TokenSettings:TokenExpirySeconds"),
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            RSAPrivateKeyBase64 = builder.Configuration.GetValue<string>("Settings:TokenSettings:PrivateKey")!,
        },
        MfaSettings = new(),
        Security = new SecuritySettingsModel
        {
            LockDownInMinutes = 0,
            LoginAttemptsForLockDown = 1000000,
            RequirePasswordChange = false
        },
        RefreshToken = new RefreshTokenSettingsModel
        {
            Audience = "adp-surveys",
            ExpireSeconds = 100000,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:RefreshTokenKey")!,
        },
        HashIdSettings = new HashIdSettings
        {
            AcceptUnencodedIds = true,
            UserIdsSalt = "s0m3-s4ndb0x-s4lt-f0r-u53r-1ds",
            UserIdsMinHashLength = 5
        },
        SASToken = new SASTokenModel
        {
            ExpiresInSeconds = 3600,
            Key = "One-Two-Three-Four-Five",
        },
        ShiftIdentityFeatureLocking = new ShiftIdentityFeatureLocking
        {
            RegionFeatureIsLocked = false,
            CityFeatureIsLocked = false,
            BrandFeatureIsLocked = false,
            DepartmentFeatureIsLocked = false,
            ServiceFeatureIsLocked = false,
            CompanyFeatureIsLocked = false,
            CompanyBranchFeatureIsLocked = false,
            AppFeatureIsLocked = false,
            AccessTreeFeatureIsLocked = false,
            UserFeatureIsLocked = false,
            TeamFeatureIsLocked = false,
        },
        DefaultDataLevelAccessOptions = new ShiftIdentityDefaultDataLevelAccessOptions
        {
            DisableDefaultCountryFilter = true
        }
    }
);

// ---------- TypeAuth (consumer registers base trees; Surveys adds its own via AddSurveysApiServices) ----------
builder.Services.AddTypeAuth(o =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<AzureStorageActionTree>();
    o.AddActionTree<GeneralActionTree>();
});

// ---------- Surveys API services ----------
builder.Services.AddSurveysApiServices<DB>(mvcBuilder, options =>
{
    options.RoutePrefix = "api/Surveys";
    options.EnableSurveysActionTreeAuthorization = true;

    // Deployment-wide branding — every survey this install serves gets this sample's
    // own identity unless the survey's own Branding overrides a field (see
    // "Sample: Multi-locale + branding" for the override case). Real consumers set
    // their own colors/logo here; the logo is served from this app's wwwroot.
    options.DefaultBranding = new ShiftSoftware.ADP.Surveys.Shared.DTOs.BrandingDto
    {
        PrimaryColor = "#EB0A1E",
        LogoUrl = "http://localhost:5134/toyota-logo.png",
    };
});

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapControllers();

// ShiftIdentity's auth + identity-server endpoints (login / refresh / MFA / auth-code,
// users, company calendar, …). These used to be classic controllers picked up by
// MapControllers() above; the 2026-07 framework release moved them to minimal APIs, which
// the host has to map explicitly. Without this call POST api/Auth/Login doesn't exist —
// the request falls through to MapFallbackToFile below and the Blazor router answers with
// its "nothing at this address" page, which looks like a routing bug rather than a missing
// endpoint. Only hosts running ShiftIdentityHostingTypes.Internal need it; hosts that
// validate tokens from a separate identity service do not, which is why nothing else in
// the estate caught this.
app.MapShiftIdentityDashboard();

// Attribute-driven CRUD for [ShiftEntityEndpoint<>] / [ShiftEntitySecureEndpoint<>]
// entities — the identity dashboard's own resources. Registered in DI by
// RegisterShiftRepositories; this maps the routes. The Surveys module's controllers are
// classic ShiftEntitySecureControllerAsync and stay on MapControllers().
app.MapShiftEntityEndpoints<DB>();

// ---------- Database create + identity seed (idempotent, dev-safe) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    await db.Database.EnsureCreatedAsync();
}

// ------------------------------------------------------------------------------------------
// PARITY BRANCH - TEMPORARY. One of exactly two edits the endpoint-parity harness is permitted
// to make outside ADP.EndpointParity/ (docs/planning/shift-framework-upgrade/
// 00-baseline-and-harness.md item B). Removed in Step 08 with the rest of the harness.
//
// The block below seeds a SuperUser, a full-access tree and a set of demo surveys, with
// IDENTITY-GENERATED primary keys, unconditionally at startup - before the harness gets a say.
// Two things break if it runs during a parity capture:
//
//   1. Those rows land in the same tables the parity cases list, so a naive "> 0 rows" gate is
//      satisfied by the demo seed ALONE and cannot distinguish "the adversarial parity seed was
//      applied" from "only the sample demo data is present". That is the exact silent failure
//      verification.md section 8.1 exists to prevent.
//   2. Rule 1 ("same longs both runs, so hash ids compare literally") holds only if these
//      seeders insert in byte-identical order into a fresh DB every time, which nobody has
//      verified - and identity PKs make it unlikely.
//
// The harness sets Parity:SuppressSampleSeeding=true and applies its own explicit-long-PK seed
// instead. Nothing else about the host changes, so the pipeline under test stays the real one.
// ------------------------------------------------------------------------------------------
var suppressSeeding = builder.Configuration.GetValue<bool>("Parity:SuppressSampleSeeding");

if (!suppressSeeding)
{

// Creates a SuperUser + minimal org hierarchy the first time the DB is empty.
// Idempotent — safe to call every startup.
await app.SeedDBAsync("SuperUser", "OneTwo", new ShiftSoftware.ShiftIdentity.Data.DBSeedOptions
{
    CountryExternalId = "1",
    CountryShortCode = "IQ",
    CountryCallingCode = "+964",

    RegionExternalId = "1",
    RegionShortCode = "KRG",

    CompanyShortCode = "SFT",
    CompanyExternalId = "-1",
    CompanyAlternativeExternalId = "shift-software",
    CompanyType = CompanyTypes.NotSpecified,

    CompanyBranchExternalId = "-11",
    CompanyBranchShortCode = "SFT-EBL"
});

await app.SetFullAccessAsync("t1", "t3");

// Seeds demo surveys (drafts only) so the builder list view ships with concrete
// examples of every advanced authoring option — banked questions, screen templates,
// branching navigation, cross-screen logic, multi-locale + branding, and triggers.
// Idempotent — each sample is keyed by IntegrationId.
await app.SeedSampleSurveysAsync();
}

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();

// ------------------------------------------------------------------------------------------
// Makes the implicit top-level-statements Program class visible to the endpoint-parity harness
// so WebApplicationFactory<Program> can boot this host in-process.
//
// Top-level statements generate an INTERNAL Program class, which WebApplicationFactory<T> in
// another assembly cannot name. This declaration only widens that visibility: it adds no
// members, changes no behaviour, and is inert at run time.
//
// TEMPORARY - one of exactly two edits the parity harness is permitted to make outside
// ADP.EndpointParity/ (docs/planning/shift-framework-upgrade/00-baseline-and-harness.md item B).
// Removed in Step 08 with the rest of the harness.
// ------------------------------------------------------------------------------------------
public partial class Program { }
