using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// A set of <see cref="MenuType"/>s as the JSON array of enum values the menus database stores, e.g.
/// <c>[0,1]</c>. Used by stores that keep the set in one text column.
/// </summary>
public static class MenuTypeList
{
    /// <summary>The types in <paramref name="json"/>, distinct and in enum order. Unknown values are skipped; null or blank is empty.</summary>
    public static List<MenuType> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<MenuType>();

        var values = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();

        return values
            .Where(value => Enum.IsDefined(typeof(MenuType), value))
            .Select(value => (MenuType)value)
            .Distinct()
            .OrderBy(menuType => menuType)
            .ToList();
    }
}
