using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Sample.API.Data;
using ShiftSoftware.ADP.Surveys.Sample.API.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftIdentity.AspNetCore;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore;
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
});

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapControllers();

// ---------- Database create + identity seed (idempotent, dev-safe) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    await db.Database.EnsureCreatedAsync();
}

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

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
