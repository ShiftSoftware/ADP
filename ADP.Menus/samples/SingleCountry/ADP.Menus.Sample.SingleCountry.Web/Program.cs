using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using ShiftSoftware.ADP.Menus.Sample.SingleCountry.Web;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuration = builder.Configuration!;

var baseUrl = configuration.GetValue<string>("BaseURL")!;
// BaseURL is expected to already include the API root segment (e.g. "http://localhost:5134/api").
// The per-module route prefix (e.g. "Menu") is supplied to AddMenuBlazorServices below and
// pre-pended by ADP.Menus.Web internally on every service / page URL.
var menuApiBaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

// The single-country sample identity server (ADP.Identity): its API (sign-in, companies, branches, countries)
// and its sign-in site.
var shiftIdentityApiURL = configuration.GetValue<string>("ShiftIdentity:BaseUrl")!;
var shiftIdentityFrontEndURL = configuration.GetValue<string>("ShiftIdentity:FrontEndBaseUrl")!;

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<TokenMessageHandlerWithAutoRefresh>())
    {
        BaseAddress = new Uri(menuApiBaseUrl)
    };

    return httpClient;
});

builder.Services.AddSingleton<IMenuCountryProvider, CountryProvider>();

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = menuApiBaseUrl;
        options.ExternalAddresses = new Dictionary<string, string?>
        {
            ["ShiftIdentityApi"] = shiftIdentityApiURL
        };
        options.UserListEndpoint = shiftIdentityApiURL.AddUrlPath("IdentityPublicUser");
        options.AddLanguage("en-US", "English");
    };
});

// Signing in redirects to the identity server's sign-in site, which sends the user back here with an auth code.
// With CookieDomain set, the refresh token lives in a cookie on that domain instead of local storage, so apps on
// the same parent domain share the session.
builder.Services.AddShiftIdentity(
    AppIdFor(configuration.GetValue<string>("ShiftIdentity:AppName")!, builder.HostEnvironment.BaseAddress, menuApiBaseUrl),
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

builder.Services.AddTypeAuth(x =>
{
    x.AddActionTree<ShiftIdentityActions>()
     .AddActionTree<AzureStorageActionTree>()
     .AddActionTree<GeneralActionTree>()
     .AddActionTree<MenuActionTree>();
});

builder.Services.AddMenuBlazorServices(options =>
{
    // The sample identity server's access trees grant the menu action tree.
    options.EnableMenuActionTreeAuthorization = true;
    options.IdentityApiUrl = shiftIdentityApiURL;
    // BaseURL already contains "/api", so we only pass the per-module segment here.
    // Must match the tail of MenuApiOptions.RoutePrefix on the API side (e.g. "api/Menu").
    options.RoutePrefix = "Menu";
    options.Languages = new()
    {
        new MenuLanguageOption("en-US", "English"),
        new MenuLanguageOption("ru-RU", "Russian"),
        new MenuLanguageOption("uz-UZ", "Uzbek"),
        new MenuLanguageOption("tg-TJ", "Tajik"),
        new MenuLanguageOption("tk-TM", "Turkmen"),
    };
});

builder.Services.AddScoped<ShiftSoftware.ADP.Menus.Sample.SingleCountry.Web.Services.TodoItemService>();

var host = builder.Build();

var setMan = host.Services.GetRequiredService<SettingManager>();

var culture = setMan.GetCulture();

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();

// Run on its own, the API serves this app from its own address; under Aspire the app has an address of its own.
// The identity server registers each address as an app, the API-served one with an "-api" suffix.
static string AppIdFor(string appId, string appAddress, string apiAddress) =>
    new Uri(appAddress).Authority == new Uri(apiAddress).Authority ? appId + "-api" : appId;
