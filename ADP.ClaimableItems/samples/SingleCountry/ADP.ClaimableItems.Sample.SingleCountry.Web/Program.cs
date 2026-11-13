using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.Web;
using ShiftSoftware.ADP.ClaimableItems.Web.Extensions;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using System.Globalization;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuration = builder.Configuration;

// This sample's API, ending in "/api/". The module's own segment ("ClaimableItems") is the RoutePrefix below.
var baseUrl = configuration.GetValue<string>("BaseURL")!;
baseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

// The external identity server: its API (sign-in, companies, branches, countries) and its sign-in site.
var shiftIdentityApiURL = configuration.GetValue<string>("ShiftIdentity:BaseURL")!;
var shiftIdentityFrontEndURL = configuration.GetValue<string>("ShiftIdentity:FrontEndBaseURL")!;

// The menus sample's API, where the contract form reads the models and basic model codes a user can pick. The
// API hands it over (Aspire gives it the address); without it the form offers no pickers.
string? menusApiUrl = null;
try
{
    using var settingsClient = new HttpClient();
    menusApiUrl = (await settingsClient.GetFromJsonAsync<SampleSettings>(baseUrl + "sample-settings"))?.MenusApiUrl;
}
catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException)
{
}

builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<TokenMessageHandlerWithAutoRefresh>())
{
    BaseAddress = new Uri(baseUrl),
});

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl;
        options.ExternalAddresses = new Dictionary<string, string?>
        {
            ["ShiftIdentityApi"] = shiftIdentityApiURL,
        };
        options.UserListEndpoint = shiftIdentityApiURL.AddUrlPath("IdentityPublicUser");
        options.AddLanguage("en-US", "English");
    };
});

// The refresh token lives in a cookie on CookieDomain, which the identity server's sign-in site writes too: a
// session opened there (or in another app on that domain) is picked up here with no redirect. The app ID is
// only used when there is no session yet.
builder.Services.AddShiftIdentity(
    configuration.GetValue<string>("ShiftIdentity:AppId")!,
    shiftIdentityApiURL,
    shiftIdentityFrontEndURL,
    configure: o =>
    {
        if (configuration.GetValue<string>("CookieDomain") is { Length: > 0 } cookieDomain)
        {
            o.RefreshTokenStorage = RefreshTokenStorage.Cookie;
            o.CookieDomain = cookieDomain;
        }
    });

builder.Services.AddTypeAuth(x => x
    .AddActionTree<ShiftIdentityActions>()
    .AddActionTree<GeneralActionTree>());

builder.Services.AddClaimableItemsBlazorServices(o =>
{
    // The tail of the API's RoutePrefix ("api/ClaimableItems"); BaseURL already ends in "/api/".
    o.RoutePrefix = "ClaimableItems";

    // The menus sample mounts its controllers under "api/Menu".
    if (!string.IsNullOrWhiteSpace(menusApiUrl))
    {
        o.MenuVehicleModelUrl = menusApiUrl.TrimEnd('/') + "/api/Menu/VehicleModel";
        o.MenuUrl = menusApiUrl.TrimEnd('/') + "/api/Menu/Menu";
    }
});

var host = builder.Build();

var culture = host.Services.GetRequiredService<SettingManager>().GetCulture();
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();

internal record SampleSettings(string? MenusApiUrl);
