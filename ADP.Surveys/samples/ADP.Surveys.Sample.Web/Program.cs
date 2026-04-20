using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.Surveys.Sample.Web;
using ShiftSoftware.ADP.Surveys.Web.Extensions;
using ShiftSoftware.ADP.Surveys.Web.WebServices;
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
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuration = builder.Configuration!;

// BaseURL is expected to include the API root segment (e.g. "http://localhost:5xxx/api").
// The per-module prefix ("Surveys") is fed to AddSurveysBlazorServices below; it's
// pre-pended by ADP.Surveys.Web on every service / page URL.
var baseUrl = configuration.GetValue<string>("BaseURL")!;
var surveysApiBaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

var shiftIdentityApiURL = configuration.GetValue<string>("ShiftIdentity:BaseURL")!;
shiftIdentityApiURL = string.IsNullOrWhiteSpace(shiftIdentityApiURL) ? baseUrl : shiftIdentityApiURL;

var shiftIdentityFrontEndURL = configuration.GetValue<string>("ShiftIdentity:FrontEndBaseUrl")!;
shiftIdentityFrontEndURL = string.IsNullOrWhiteSpace(shiftIdentityFrontEndURL) ? baseUrl : shiftIdentityFrontEndURL;

// HttpClient with automatic token refresh — every outbound call picks up the current
// JWT, and a refresh happens transparently when the token is near expiry.
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<TokenMessageHandlerWithAutoRefresh>())
    {
        BaseAddress = new Uri(surveysApiBaseUrl),
    };
    return httpClient;
});

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = surveysApiBaseUrl;
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

// Internal hosting — the Sample.API hosts ShiftIdentity in-process (per Menus precedent).
// For "real" consumers who run identity separately, switch to External.
builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal;
    x.Title = "ADP.Surveys";
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

builder.Services.AddSurveysBlazorServices(options =>
{
    options.EnableSurveysActionTreeAuthorization = false;
    // BaseURL already ends in "/api/", so we only pass the per-module segment here.
    // Must match the tail of SurveyApiOptions.RoutePrefix on the API side ("api/Surveys").
    options.RoutePrefix = "Surveys";
});

var host = builder.Build();

var setMan = host.Services.GetRequiredService<SettingManager>();
var culture = setMan.GetCulture();
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();
