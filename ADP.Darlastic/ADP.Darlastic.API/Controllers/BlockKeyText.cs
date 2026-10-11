namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>How a comparison group (an engine blocking key such as "P:&lt;phone&gt;" or "R:&lt;source&gt;|&lt;id&gt;") reads in the case browser.</summary>
public static class BlockKeyText
{
    /// <summary>
    /// A record-reference group ("R:&lt;source&gt;|&lt;record id&gt;") with the source's plain name instead of its
    /// slug. Any other group, or a source <paramref name="labels"/> does not name, is returned as it is.
    /// </summary>
    public static string WithSourceLabel(string key, IReadOnlyDictionary<string, string>? labels)
    {
        if (labels is null || labels.Count == 0 || !key.StartsWith("R:", StringComparison.Ordinal)) return key;
        int bar = key.IndexOf('|', 2);
        string slug = bar < 0 ? key[2..] : key[2..bar];
        return labels.TryGetValue(slug, out var label) && !string.IsNullOrWhiteSpace(label)
            ? "R:" + label + (bar < 0 ? "" : key[bar..])
            : key;
    }
}
