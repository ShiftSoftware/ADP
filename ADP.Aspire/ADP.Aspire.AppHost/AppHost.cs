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
void AddFunctions<TProject>(string name, int port) where TProject : IProjectMetadata, new() =>
    builder.AddProject<TProject>(name)
        .WithHttpEndpoint(port: port, isProxied: false)
        .WithArgs("--port", port.ToString())
        .WithRelationship(azurite.Resource, "storage");

// Each sample API serves its Blazor app when run on its own; here the Blazor app is a resource of its own.
void AddApi<TProject>(string name) where TProject : IProjectMetadata, new() =>
    builder.AddProject<TProject>(name).WithEnvironment("Sample__HostBlazorClient", "false");

// Menus — single country
AddApi<Projects.ADP_Menus_Sample_SingleCountry_API>("menus-single-country-api");
builder.AddProject<Projects.ADP_Menus_Sample_SingleCountry_Web>("menus-single-country-web");
AddFunctions<Projects.ADP_Menus_Sample_SingleCountry_Functions>("menus-single-country-functions", 7071);

// Menus — multi country
AddApi<Projects.ADP_Menus_Sample_MultiCountry_API>("menus-multi-country-api");
builder.AddProject<Projects.ADP_Menus_Sample_MultiCountry_Web>("menus-multi-country-web");
AddFunctions<Projects.ADP_Menus_Sample_MultiCountry_Functions>("menus-multi-country-functions", 7072);

// Claimable items — single country
AddApi<Projects.ADP_ClaimableItems_Sample_SingleCountry_API>("claimable-items-single-country-api");
builder.AddProject<Projects.ADP_ClaimableItems_Sample_SingleCountry_Web>("claimable-items-single-country-web");

// Claimable items — multi country
AddApi<Projects.ADP_ClaimableItems_Sample_MultiCountry_API>("claimable-items-multi-country-api");
builder.AddProject<Projects.ADP_ClaimableItems_Sample_MultiCountry_Web>("claimable-items-multi-country-web");

builder.Build().Run();
