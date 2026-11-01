using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.Web;
using ShiftSoftware.ADP.ClaimableItems.Web.Extensions;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using System.Globalization;

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

builder.Services.AddShiftIdentity(
    configuration.GetValue<string>("ShiftIdentity:AppId")!,
    shiftIdentityApiURL,
    shiftIdentityFrontEndURL);

builder.Services.AddTypeAuth(x => x
    .AddActionTree<ShiftIdentityActions>()
    .AddActionTree<GeneralActionTree>());

builder.Services.AddClaimableItemsBlazorServices(o =>
{
    // The tail of the API's RoutePrefix ("api/ClaimableItems"); BaseURL already ends in "/api/".
    o.RoutePrefix = "ClaimableItems";
});

var host = builder.Build();

var culture = host.Services.GetRequiredService<SettingManager>().GetCulture();
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();
