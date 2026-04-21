using ShiftSoftware.ADP.Menus.Sample.API.Data;
using ShiftSoftware.ADP.Menus.Sample.API.DataServices;
using ShiftSoftware.ADP.Menus.Sample.API.Services;
using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Shared;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftIdentity.AspNetCore;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DB>(db =>
    db.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")));

// ---------- Cosmos / Menu interface implementations ----------
var cosmosConnectionString = builder.Configuration.GetConnectionString("Cosmos");
if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
    builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));

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

// ---------- ShiftEntity Web (HashId, AzureStorage, AutoMapper, data assemblies) ----------
var azureStorageAccounts = new List<AzureStorageOption>();
builder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(ShiftSoftware.ADP.Menus.Sample.API.Data.DB).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);
    x.AddAutoMapper(typeof(ShiftSoftware.ADP.Menus.Sample.API.Data.DB).Assembly);

    x.HashId.RegisterHashId(builder.Configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    x.HashId.RegisterIdentityHashId(
        builder.Configuration.GetValue<string>("Settings:IdentityHashIdSettings:Salt") ?? "",
        builder.Configuration.GetValue<int>("Settings:IdentityHashIdSettings:MinHashLength")
    );

    x.AddAzureStorage(azureStorageAccounts.ToArray());
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
            Audience = "adp-menus",
            ExpireSeconds = 100000,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:RefreshTokenKey")!,
        },
        HashIdSettings = new HashIdSettings
        {
            AcceptUnencodedIds = true,
            UserIdsSalt = "k02iUHSb2ier9fiui02349AbfJEI",
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

// ---------- Consumer Todo repository ----------
builder.Services.AddScoped<ShiftSoftware.ADP.Menus.Sample.API.Data.Repositories.TodoItemRepository>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    // Sample reset: drop and recreate on every startup so the schema always matches
    // the current DbContext shape (identity tables included). Safe because this is a
    // throwaway sample DB — don't copy this pattern into real projects.
    if (app.Environment.IsDevelopment())
    {
        //await db.Database.EnsureDeletedAsync();
    }
    await db.Database.EnsureCreatedAsync();
    await db.SeedAsync();
}

// Seed identity (SuperUser, Country, Region, Company, Branch) and grant full access.
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

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
