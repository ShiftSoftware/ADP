using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// The Functions samples' local.settings.json use UseDevelopmentStorage=true, so Azurite runs as a plain process
// (via npx) rather than through the Azure Storage integration, which needs a container runtime.
string[] azuriteArgs =
[
    "--yes", "azurite", "--silent", "--skipApiVersionCheck",
    "--location", Path.Combine(Path.GetTempPath(), "adp-aspire-azurite"),
];
var azurite = OperatingSystem.IsWindows()
    ? builder.AddExecutable("azurite", "cmd", builder.AppHostDirectory, ["/c", "npx", .. azuriteArgs])
    : builder.AddExecutable("azurite", "npx", builder.AppHostDirectory, azuriteArgs);

// Plain AddProject for the Functions samples: `dotnet run` on a Functions worker project runs `func start`
// (Azure Functions Core Tools).
IResourceBuilder<ProjectResource> AddFunctions<TProject>(string name, int port) where TProject : IProjectMetadata, new() =>
    builder.AddProject<TProject>(name)
        .WithHttpEndpoint(port: port, isProxied: false)
        .WithArgs("--port", port.ToString())
        .WithRelationship(azurite.Resource, "storage");

// Each sample API serves its Blazor app when run on its own; here the Blazor app is a resource of its own.
IResourceBuilder<ProjectResource> AddApi<TProject>(string name) where TProject : IProjectMetadata, new() =>
    builder.AddProject<TProject>(name).WithEnvironment("Sample__HostBlazorClient", "false");

// A claimable-items sample reads its vehicles and contract menus from the menus sample of the same shape: the
// lookup functions server to server, and the menus API from the browser (handed on by the API's sample-settings).
void UseMenus(
    IResourceBuilder<ProjectResource> claimableItemsApi,
    IResourceBuilder<ProjectResource> menusApi,
    IResourceBuilder<ProjectResource> menusFunctions) =>
    claimableItemsApi
        .WithEnvironment("Sample__MenuLookupUrl", menusFunctions.GetEndpoint("http"))
        .WithEnvironment("Sample__MenusApiUrl", menusApi.GetEndpoint("http"))
        .WithRelationship(menusFunctions.Resource, "menu lookup")
        .WithRelationship(menusApi.Resource, "menus");

// Run on its own, a menus sample hosts a fake identity. Here it signs in on the external identity server of the
// claimable-items sample of the same shape, read from that sample's development settings, so one sign-in covers
// both and the contract form can read the menus API. The app ID must be registered on that server with the menus
// Blazor app's URL as its redirect.
void UseIdentityOf<TClaimableItemsApi, TClaimableItemsWeb>(IResourceBuilder<ProjectResource> menusApi, string appIdKey)
    where TClaimableItemsApi : IProjectMetadata, new()
    where TClaimableItemsWeb : IProjectMetadata, new()
{
    IConfiguration Settings(string projectPath, params string[] file) => new ConfigurationBuilder()
        .AddJsonFile(Path.Combine([Path.GetDirectoryName(projectPath)!, .. file]), optional: true)
        .Build();

    var api = Settings(new TClaimableItemsApi().ProjectPath, "appsettings.Development.json").GetSection("ShiftIdentity");
    var web = Settings(new TClaimableItemsWeb().ProjectPath, "wwwroot", "appsettings.Development.json").GetSection("ShiftIdentity");

    if (string.IsNullOrWhiteSpace(api["Issuer"]))
        return;

    menusApi
        .WithEnvironment("Sample__ExternalIdentity__Issuer", api["Issuer"])
        .WithEnvironment("Sample__ExternalIdentity__PublicKey", api["PublicKey"])
        .WithEnvironment("Sample__ExternalIdentity__HashIds__Salt", api["HashIds:Salt"])
        .WithEnvironment("Sample__ExternalIdentity__HashIds__MinHashLength", api["HashIds:MinHashLength"])
        .WithEnvironment("Sample__ExternalIdentity__AppId", builder.Configuration[appIdKey] ?? "menus-sample-dev")
        .WithEnvironment("Sample__ExternalIdentity__BaseUrl", web["BaseURL"])
        .WithEnvironment("Sample__ExternalIdentity__FrontEndBaseUrl", web["FrontEndBaseURL"]);
}

// Menus — single country
var menusSingleApi = AddApi<Projects.ADP_Menus_Sample_SingleCountry_API>("menus-single-country-api");
builder.AddProject<Projects.ADP_Menus_Sample_SingleCountry_Web>("menus-single-country-web");
UseIdentityOf<Projects.ADP_ClaimableItems_Sample_SingleCountry_API, Projects.ADP_ClaimableItems_Sample_SingleCountry_Web>(menusSingleApi, "MenusSamples:SingleCountry:IdentityAppId");
var menusSingleFunctions = AddFunctions<Projects.ADP_Menus_Sample_SingleCountry_Functions>("menus-single-country-functions", 7071);

// Menus — multi country
var menusMultiApi = AddApi<Projects.ADP_Menus_Sample_MultiCountry_API>("menus-multi-country-api");
builder.AddProject<Projects.ADP_Menus_Sample_MultiCountry_Web>("menus-multi-country-web");
UseIdentityOf<Projects.ADP_ClaimableItems_Sample_MultiCountry_API, Projects.ADP_ClaimableItems_Sample_MultiCountry_Web>(menusMultiApi, "MenusSamples:MultiCountry:IdentityAppId");
var menusMultiFunctions = AddFunctions<Projects.ADP_Menus_Sample_MultiCountry_Functions>("menus-multi-country-functions", 7072);

// Claimable items — single country
UseMenus(AddApi<Projects.ADP_ClaimableItems_Sample_SingleCountry_API>("claimable-items-single-country-api"), menusSingleApi, menusSingleFunctions);
builder.AddProject<Projects.ADP_ClaimableItems_Sample_SingleCountry_Web>("claimable-items-single-country-web");

// Claimable items — multi country
UseMenus(AddApi<Projects.ADP_ClaimableItems_Sample_MultiCountry_API>("claimable-items-multi-country-api"), menusMultiApi, menusMultiFunctions);
builder.AddProject<Projects.ADP_ClaimableItems_Sample_MultiCountry_Web>("claimable-items-multi-country-web");

builder.Build().Run();
