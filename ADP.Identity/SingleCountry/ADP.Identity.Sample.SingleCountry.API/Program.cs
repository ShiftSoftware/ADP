using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;
using ShiftSoftware.ADP.Identity.Sample.SingleCountry.API.Data;
using ShiftSoftware.ADP.Identity.Sample.SingleCountry.API.Services;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Models;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

// The identity API of a single-country distributor, for the menus and claimable-items samples of that shape.
// Its sign-in app and dashboard are the .Web project next to it. Its database is SQLite in memory: created and seeded from identity-seed.json at startup, gone when it stops.

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDbContext<DB>(x => x.UseSqlite(configuration.GetConnectionString("Identity")));

builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

var mvcBuilder = builder.Services.AddControllers();

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(DB).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);

    x.HashId.RegisterHashId(configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    // The apps that trust this server decode company, branch and country IDs with the same salt.
    x.HashId.RegisterIdentityHashId(
        configuration["Settings:IdentityHashIdSettings:Salt"] ?? "",
        configuration.GetValue<int>("Settings:IdentityHashIdSettings:MinHashLength"));

    x.AddShiftIdentityDataAssembly();
});

mvcBuilder.AddShiftIdentity(configuration["Settings:TokenSettings:Issuer"]!, configuration["Settings:TokenSettings:PublicKey"]!);

builder.Services.AddScoped<ISendEmailVerification, ConsoleEmailService>();
builder.Services.AddScoped<ISendEmailResetPassword, ConsoleEmailService>();

mvcBuilder.AddShiftIdentityDashboard<DB>(new ShiftIdentityConfiguration
{
    ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal,
    // Where the sign-in app is served, for the links in security emails.
    FrontEndUrl = configuration["Settings:FrontEndUrl"],
    Authority = configuration.GetSection("Settings:Authority").Get<AuthoritySettingsModel>() ?? new(),
    FactorProtection = configuration.GetSection("Settings:FactorProtection").Get<FactorProtectionSettings>() ?? new(),
    Token = new TokenSettingsModel
    {
        ExpireSeconds = configuration.GetValue<int>("Settings:TokenSettings:TokenExpirySeconds"),
        Issuer = configuration["Settings:TokenSettings:Issuer"]!,
        RSAPrivateKeyBase64 = configuration["Settings:TokenSettings:PrivateKey"]!,
    },
    Security = new SecuritySettingsModel
    {
        LockDownInMinutes = 0,
        LoginAttemptsForLockDown = 1000000,
        RequirePasswordChange = false,
    },
    MfaSettings = new(),
    RefreshToken = new RefreshTokenSettingsModel
    {
        Audience = "adp-identity-single-country",
        ExpireSeconds = 100000,
        Issuer = configuration["Settings:TokenSettings:Issuer"]!,
        Key = configuration["Settings:TokenSettings:RefreshTokenKey"]!,
    },
    HashIdSettings = new HashIdSettings
    {
        AcceptUnencodedIds = true,
        UserIdsSalt = configuration["Settings:UserHashIdSettings:Salt"]!,
        UserIdsMinHashLength = configuration.GetValue<int>("Settings:UserHashIdSettings:MinHashLength"),
    },
    SASToken = new SASTokenModel
    {
        ExpiresInSeconds = 3600,
        Key = configuration["Settings:SASTokenKey"]!,
    },
    ShiftIdentityFeatureLocking = new ShiftIdentityFeatureLocking(),
    DefaultDataLevelAccessOptions = new ShiftIdentityDefaultDataLevelAccessOptions
    {
        DisableDefaultCountryFilter = true,
    },
});

builder.Services.AddTypeAuth(o =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<GeneralActionTree>();
    o.AddActionTree<MenuActionTree>();
    o.AddActionTree<ClaimableItemsActionTree>();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DB>().Database.EnsureCreatedAsync();

await app.SeedShiftIdentitySqliteAsync(await File.ReadAllTextAsync(Path.Combine(app.Environment.ContentRootPath, "identity-seed.json")));

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapShiftIdentityDashboard();
app.MapShiftEntityEndpoints<DB>();

app.Run();
