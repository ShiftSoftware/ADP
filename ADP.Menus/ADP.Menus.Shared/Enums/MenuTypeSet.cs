using System.Text.Json;

namespace ShiftSoftware.ADP.Menus.Shared.Enums;

/// <summary>
/// Helpers for a set of <see cref="MenuType"/>s held as a list. A variant is offered under at least one
/// type, and each of its service intervals and standalone items lists the types it is visible under. Only
/// the lookup reads that — the export generates the same lines whatever the lists hold.
///
/// <para>The menus database stores a list as a JSON array of the enum values, e.g. <c>[0,1]</c> (an EF
/// primitive collection, no configuration); <see cref="ToJson"/> writes that same text for stores that copy it.</para>
/// </summary>
public static class MenuTypeSet
{
    /// <summary>Distinct, defined types in enum order.</summary>
    public static List<MenuType> Normalize(IEnumerable<MenuType>? menuTypes) =>
        (menuTypes ?? []).Where(Enum.IsDefined).Distinct().Order().ToList();

    /// <summary>
    /// The types a period or standalone item is visible under: its own, limited to the variant's, or all of the
    /// variant's when it lists none.
    /// </summary>
    public static List<MenuType> Resolve(IEnumerable<MenuType>? own, IEnumerable<MenuType> variantTypes)
    {
        var variant = Normalize(variantTypes);
        var types = Normalize(own).Where(variant.Contains).ToList();

        return types.Count > 0 ? types : variant;
    }

    public static bool SameAs(this IEnumerable<MenuType>? left, IEnumerable<MenuType>? right) =>
        Normalize(left).SequenceEqual(Normalize(right));

    public static string ToJson(IEnumerable<MenuType>? menuTypes) =>
        JsonSerializer.Serialize(Normalize(menuTypes).Select(menuType => (int)menuType));
}
