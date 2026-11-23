using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;
using ShiftSoftware.ADP.Identity.Sample.SingleCountry.Web;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The identity API, ending in "/api/". This app is the sign-in site, on its own address.
var apiUrl = builder.Configuration.GetValue<string>("BaseURL")!;
apiUrl = apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/";
var origin = builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<TokenMessageHandlerWithAutoRefresh>())
{
    BaseAddress = new Uri(apiUrl),
});

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = apiUrl;
        options.ExternalAddresses = new Dictionary<string, string?> { ["ShiftIdentityApi"] = apiUrl };
        options.UserListEndpoint = apiUrl.AddUrlPath("IdentityPublicUser");
        options.AddLanguage("en-US", "English");
    };
});

builder.Services.AddShiftIdentity("identity-single-country", apiUrl, origin.TrimEnd('/'));

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal;
    x.LogoPath = "/img/shift-full.png";
    x.Title = "ADP Identity (single country)";
    // Must match the API's Settings:Authority:Enabled: sign-in lives on the authority's routes.
    x.StagedAuthority = true;
    x.DynamicTypeAuthActionExpander = () => Task.CompletedTask;
});

builder.Services.AddTypeAuth(x => x
    .AddActionTree<ShiftIdentityActions>()
    .AddActionTree<GeneralActionTree>()
    .AddActionTree<MenuActionTree>()
    .AddActionTree<ClaimableItemsActionTree>());

var host = builder.Build();

var culture = host.Services.GetRequiredService<SettingManager>().GetCulture();
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();
