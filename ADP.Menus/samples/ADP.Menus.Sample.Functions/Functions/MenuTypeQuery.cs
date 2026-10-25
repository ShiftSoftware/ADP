using Microsoft.AspNetCore.Http;

using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Menus.Sample.Functions.Functions;

/// <summary>
/// Binds the <c>menuTypes</c> query parameter the lookup functions share: comma-separated, repeatable, or
/// both — <c>?menuTypes=Free,Contract</c> and <c>?menuTypes=Free&amp;menuTypes=Contract</c> mean the same.
/// </summary>
internal static class MenuTypeQuery
{
    /// <summary>
    /// The menu types named on the request, or null when none parse. An unparseable name is dropped rather
    /// than 400-ing: this narrows a result set, so the safe failure is returning too much, not too little.
    /// </summary>
    public static List<MenuType>? Parse(HttpRequest request)
    {
        var menuTypes = request.Query["menuTypes"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(name => Enum.TryParse<MenuType>(name, ignoreCase: true, out var menuType) && Enum.IsDefined(menuType) ? menuType : (MenuType?)null)
            .OfType<MenuType>()
            .Distinct()
            .ToList();

        return menuTypes.Count > 0 ? menuTypes : null;
    }
}
