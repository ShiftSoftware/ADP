using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ADP.Menus.Web.WebServices;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using ShiftSoftware.ADP.Menus.Sample.Web;
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

var shiftIdentityApiURL = configuration.GetValue<string>("ShiftIdentity:BaseURL")!;
shiftIdentityApiURL = string.IsNullOrWhiteSpace(shiftIdentityApiURL) ? baseUrl : shiftIdentityApiURL;

var shiftIdentityFrontEndURL = configuration.GetValue<string>("ShiftIdentity:FrontEndBaseUrl")!;
shiftIdentityFrontEndURL = string.IsNullOrWhiteSpace(shiftIdentityFrontEndURL) ? baseUrl : shiftIdentityFrontEndURL;

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

builder.Services.AddShiftIdentity(
    configuration.GetValue<string>("ShiftIdentity:AppName")!,
    shiftIdentityApiURL,
    shiftIdentityFrontEndURL);

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftIdentityHostingTypes.External;
    x.LogoPath = "/img/shift-full.png";
    x.Title = "ADP.Menus";
    x.DynamicTypeAuthActionExpander = () => Task.CompletedTask;
});

builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<IIdentityStore, TokenStorageService>();

builder.Services.AddTypeAuth(x =>
{
    x.AddActionTree<ShiftIdentityActions>()
     .AddActionTree<AzureStorageActionTree>()
     .AddActionTree<GeneralActionTree>();
});

builder.Services.AddMenuBlazorServices(options =>
{
    options.EnableMenuActionTreeAuthorization = false;
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

builder.Services.AddScoped<ShiftSoftware.ADP.Menus.Sample.Web.Services.TodoItemService>();

var host = builder.Build();

var setMan = host.Services.GetRequiredService<SettingManager>();

var culture = setMan.GetCulture();

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();
