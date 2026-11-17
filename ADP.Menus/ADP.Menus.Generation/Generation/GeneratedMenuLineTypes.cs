using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// Reads generated lines by menu type. The generator emits each line ONCE, carrying every type it is
/// offered under; a consumer that presents menus per type (the lookup) splits them here, and one that
/// does not (the DMS export) never calls this.
/// </summary>
public static class GeneratedMenuLineTypes
{
    /// <summary>
    /// One line per (line, menu type): a line offered under two types comes back twice, identical but for
    /// <see cref="GeneratedMenuLine.MenuType"/> and — on a periodic line — the description, which is the
    /// interval's description for that type. <see cref="GeneratedMenuLine.LineKey"/> is NOT changed, so
    /// within the result a line is identified by its key together with its type.
    /// </summary>
    /// <param name="menuTypes">The types to keep; null or empty keeps every type.</param>
    public static List<GeneratedMenuLine> Split(
        IEnumerable<GeneratedMenuLine> lines,
        MenuGenerationReferenceData reference,
        IReadOnlyCollection<MenuType> menuTypes = null)
    {
        var result = new List<GeneratedMenuLine>();

        foreach (var line in lines)
        {
            foreach (var menuType in line.MenuTypes)
            {
                if (menuTypes is { Count: > 0 } && !menuTypes.Contains(menuType))
                    continue;

                var copy = line.Copy();
                copy.MenuType = menuType;

                if (line.LineType == MenuLineType.Periodic
                    && line.ServiceIntervalID.HasValue
                    && reference.Intervals.TryGetValue(line.ServiceIntervalID.Value, out var interval))
                    copy.Description = interval.DescriptionFor(menuType);

                result.Add(copy);
            }
        }

        return result;
    }
}
