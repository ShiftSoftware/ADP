using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Data;
using ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Services;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

// A host shaped like a single-country distributor: one country, so the contract form never asks for one.
// Identity is EXTERNAL - users sign in on an existing ShiftIdentity server, and this API only validates its
// tokens. Its development issuer, public key and hash-id salt are in appsettings.Development.json (see README.md).

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Checked after Build() rather than here, so `dotnet ef` (which stops at Build) works without them.
string[] requiredSettings =
[
    "ShiftIdentity:Issuer",
    "ShiftIdentity:PublicKey",
    "ConnectionStrings:Cosmos",
];
var missingSettings = requiredSettings.Where(x => string.IsNullOrWhiteSpace(configuration[x])).ToList();

// ---------- Database ----------
builder.Services.AddDbContext<DB>(x => x
    .UseSqlServer(configuration.GetConnectionString("SQLServer"))
    .UseTemporal(true));

// The modules' tables, under their own schemas.
builder.Services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor());

// ---------- ASP.NET + ShiftEntity ----------
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = configuration["Sample:PrintSASTokenKey"] ?? "sample-print-key";
});

var mvcBuilder = builder.Services.AddControllers();

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(DB).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);

    // Must match the identity server, or company, branch and country IDs will not decode.
    x.HashId.RegisterHashId(configuration.GetValue<bool>("ShiftIdentity:HashIds:AcceptUnencodedIds"));
    x.HashId.RegisterIdentityHashId(
        configuration["ShiftIdentity:HashIds:Salt"] ?? "",
        configuration.GetValue<int>("ShiftIdentity:HashIds:MinHashLength"));
});

// ---------- External identity: validate the identity server's tokens ----------
if (missingSettings.Count == 0)
    mvcBuilder.AddShiftIdentity(configuration["ShiftIdentity:Issuer"]!, configuration["ShiftIdentity:PublicKey"]!);

// The access tree comes from the identity server; the module's own tree is not on it, so this sample leaves
// the contract permissions off (see EnableClaimableItemsActionTreeAuthorization below).
builder.Services.AddTypeAuth(o =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<GeneralActionTree>();
});

// ---------- Vehicle lookup on Cosmos (the contract menu comes from it) ----------
if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Cosmos")))
    builder.Services.AddSingleton(new CosmosClient(configuration.GetConnectionString("Cosmos")));

builder.Services.AddLookupService(o =>
{
    o.VehicleLookupStorageSource = StorageSources.CosmosDB;
    o.CosmosDatabaseNameSuffix = configuration["Lookup:CosmosDatabaseNameSuffix"];

    // One country: the menu is priced for it even where a caller does not pass it.
    o.ConfigureServiceMenu = menu => menu.DefaultCountryID = configuration.GetValue<long?>("Sample:Country:Id");
});

builder.Services.AddSingleton<IMenuCountryProvider, SampleCountryProvider>();

// ---------- Claimable Items (service contracts) ----------
builder.Services.AddScoped<ICompanyInfoProvider, SampleCompanyInfoProvider>();

// The module's claim repositories are registered with the contract one and need this, even unused here.
builder.Services.AddSingleton<SharedClaimService>();

builder.Services.AddClaimableItemsApiServices<DB>(mvcBuilder, o =>
{
    o.RoutePrefix = "api/ClaimableItems";
    o.EnableClaimableItemsActionTreeAuthorization = false;
    o.ContractMinimumLines = configuration.GetValue("Sample:ContractMinimumLines", 1);
    o.ContractPeriodicMileageMargin = configuration.GetValue<long>("Sample:ContractPeriodicMileageMargin");

    if (configuration["Sample:TimeZone"] is { Length: > 0 } timeZone)
        o.ContractTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (missingSettings.Count > 0)
    throw new InvalidOperationException(
        $"Set these in the API's appsettings.Development.json before running the sample: {string.Join(", ", missingSettings)}. See README.md.");

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DB>().Database.MigrateAsync();

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
