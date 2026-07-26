using System.Text.Json;

namespace ShiftSoftware.ADP.Surveys.Shared;

/// <summary>
/// Cell-level formatting for the response export. Pure string work, kept out of the
/// exporter service so the rules that matter — formula neutralisation above all — can be
/// pinned by tests without a database.
/// </summary>
public static class SurveyCsv
{
    /// <summary>
    /// RFC 4180 quoting, plus formula neutralisation.
    /// </summary>
    /// <remarks>
    /// The leading-character guard is the security-relevant half. Survey answers are free
    /// text submitted by anonymous respondents, and a cell beginning <c>=</c>, <c>+</c>,
    /// <c>-</c> or <c>@</c> is evaluated as a formula the moment an analyst opens the file
    /// in Excel — the standard CSV-injection path to running commands on their machine.
    /// Prefixing an apostrophe forces Excel to treat the cell as text; the apostrophe is
    /// not displayed.
    /// </remarks>
    public static string EscapeCell(string? cell)
    {
        var value = cell ?? "";

        if (value.Length > 0 && (value[0] is '=' or '+' or '-' or '@'))
            value = "'" + value;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    /// <summary>
    /// Flattens a stored answer's JSON to one cell. Strings shed their quotes, arrays
    /// become a delimited list, anything else keeps its raw JSON so a signature data URL
    /// or object-shaped answer survives instead of degrading to a type name.
    /// Non-JSON input is passed through rather than dropped — losing an answer to a
    /// formatting decision is worse than an ugly cell.
    /// </summary>
    public static string RenderValue(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) return "";

        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            return RenderElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return valueJson;
        }
    }

    private static string RenderElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        JsonValueKind.Array => string.Join(" | ", element.EnumerateArray().Select(RenderElement)),
        _ => element.GetRawText(),
    };
}
