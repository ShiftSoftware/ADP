using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ADP.Menus.Web.Extensions;

public class MenuWebOptions
{
    /// <summary>
    /// Optional custom layout type for Menu pages. Must be a Blazor LayoutComponentBase.
    /// When null, Menu pages use the built-in MenuLayout.
    /// </summary>
    public Type? Layout { get; set; }

    /// <summary>
    /// When true, Menu Blazor UI hides actions/nav items the user does not have MenuActionTree permission for.
    /// When false (default), UI behaves as before â€” only authentication is required.
    /// </summary>
    public bool EnableMenuActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Optional list of languages supported by multi-language fields (StandaloneOperationCode, MenuCode).
    /// When empty, the corresponding forms render plain text inputs and the report export forms hide the language selector.
    /// </summary>
    public List<MenuLanguageOption> Languages { get; set; } = new();

    /// <summary>
    /// Returns <see cref="Languages"/> converted to <see cref="LanguageInfo"/> for use with <c>LocalizedTextField</c>.
    /// </summary>
    internal List<LanguageInfo> LanguageInfos =>
        Languages.Select(x => new LanguageInfo { CultureName = x.Culture, Label = x.Label }).ToList();

    /// <summary>
    /// Base URL of the ShiftIdentity API. Used by Menu list/form pages to look up brands
    /// (and other identity-owned data) without depending on ShiftBlazor's ExternalAddresses map.
    /// </summary>
    public string? IdentityApiUrl { get; set; }

    /// <summary>
    /// Route prefix that Menu API controllers are mounted under (relative to the HttpClient base address).
    /// For example if HttpClient.BaseAddress already contains "/api/" and the Menu API uses
    /// RoutePrefix "api/Menu", set this to "Menu" so calls resolve to "/api/Menu/...".
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="RoutePrefix"/> normalized with a trailing slash, or an empty string when no prefix is set.
    /// Pre-pend this to any relative URL (HttpClient call, ShiftList EntitySet, ShiftEntityForm Endpoint)
    /// that targets the Menu API.
    /// </summary>
    public string ResolvedRoutePrefix =>
        string.IsNullOrWhiteSpace(RoutePrefix) ? string.Empty : RoutePrefix.Trim('/') + "/";
}

public record MenuLanguageOption(string Culture, string Label);
