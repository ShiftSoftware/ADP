var builder = DistributedApplication.CreateBuilder(args);

// Menus
// The Functions sample's local.settings.json uses UseDevelopmentStorage=true, so Azurite runs as a plain
// process (via npx) rather than through the Azure Storage integration, which needs a container runtime.
string[] azuriteArgs =
[
    "--yes", "azurite", "--silent", "--skipApiVersionCheck",
    "--location", Path.Combine(Path.GetTempPath(), "adp-aspire-azurite"),
];
var azurite = OperatingSystem.IsWindows()
    ? builder.AddExecutable("azurite", "cmd", builder.AppHostDirectory, ["/c", "npx", .. azuriteArgs])
    : builder.AddExecutable("azurite", "npx", builder.AppHostDirectory, azuriteArgs);

builder.AddProject<Projects.ADP_Menus_Sample_API>("menus-api");

builder.AddProject<Projects.ADP_Menus_Sample_Web>("menus-web");

// Plain AddProject: `dotnet run` on a Functions worker project runs `func start` (Azure Functions Core Tools).
builder.AddProject<Projects.ADP_Menus_Sample_Functions>("menus-functions")
    .WithHttpEndpoint(port: 7071, isProxied: false)
    .WithArgs("--port", "7071")
    .WithRelationship(azurite.Resource, "storage");

// Claimable items — single country
builder.AddProject<Projects.ADP_ClaimableItems_Sample_SingleCountry_API>("claimable-items-single-country-api");

builder.AddProject<Projects.ADP_ClaimableItems_Sample_SingleCountry_Web>("claimable-items-single-country-web");

// Claimable items — multi country
builder.AddProject<Projects.ADP_ClaimableItems_Sample_MultiCountry_API>("claimable-items-multi-country-api");

builder.AddProject<Projects.ADP_ClaimableItems_Sample_MultiCountry_Web>("claimable-items-multi-country-web");

builder.Build().Run();
