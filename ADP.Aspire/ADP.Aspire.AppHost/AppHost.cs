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

// The sample identity servers (ADP.Identity), one per shape: an API that keeps its users, companies and countries in
// SQLite in memory, seeded at startup, and a Blazor app that is its sign-in site and dashboard. The samples of the
// same shape trust its tokens, with its issuer, public key and hash-id salt in their development settings.
IResourceBuilder<ProjectResource> UseIdentity(IResourceBuilder<ProjectResource> project, IResourceBuilder<ProjectResource> identity) =>
    project.WaitFor(identity).WithRelationship(identity.Resource, "identity");

var identitySingle = builder.AddProject<Projects.ADP_Identity_Sample_SingleCountry_API>("identity-single-country-api");
UseIdentity(builder.AddProject<Projects.ADP_Identity_Sample_SingleCountry_Web>("identity-single-country-web"), identitySingle);
var identityMulti = builder.AddProject<Projects.ADP_Identity_Sample_MultiCountry_API>("identity-multi-country-api");
UseIdentity(builder.AddProject<Projects.ADP_Identity_Sample_MultiCountry_Web>("identity-multi-country-web"), identityMulti);

// Menus — single country
var menusSingleApi = UseIdentity(AddApi<Projects.ADP_Menus_Sample_SingleCountry_API>("menus-single-country-api"), identitySingle);
UseIdentity(builder.AddProject<Projects.ADP_Menus_Sample_SingleCountry_Web>("menus-single-country-web"), identitySingle);
var menusSingleFunctions = AddFunctions<Projects.ADP_Menus_Sample_SingleCountry_Functions>("menus-single-country-functions", 7071);

// Menus — multi country
var menusMultiApi = UseIdentity(AddApi<Projects.ADP_Menus_Sample_MultiCountry_API>("menus-multi-country-api"), identityMulti);
UseIdentity(builder.AddProject<Projects.ADP_Menus_Sample_MultiCountry_Web>("menus-multi-country-web"), identityMulti);
var menusMultiFunctions = AddFunctions<Projects.ADP_Menus_Sample_MultiCountry_Functions>("menus-multi-country-functions", 7072);

// Claimable items — single country
UseMenus(UseIdentity(AddApi<Projects.ADP_ClaimableItems_Sample_SingleCountry_API>("claimable-items-single-country-api"), identitySingle), menusSingleApi, menusSingleFunctions);
UseIdentity(builder.AddProject<Projects.ADP_ClaimableItems_Sample_SingleCountry_Web>("claimable-items-single-country-web"), identitySingle);

// Claimable items — multi country
UseMenus(UseIdentity(AddApi<Projects.ADP_ClaimableItems_Sample_MultiCountry_API>("claimable-items-multi-country-api"), identityMulti), menusMultiApi, menusMultiFunctions);
UseIdentity(builder.AddProject<Projects.ADP_ClaimableItems_Sample_MultiCountry_Web>("claimable-items-multi-country-web"), identityMulti);

builder.Build().Run();
