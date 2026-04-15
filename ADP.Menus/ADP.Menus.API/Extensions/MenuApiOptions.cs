namespace ShiftSoftware.ADP.Menus.API.Extensions;

public class MenuApiOptions
{
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// When true, Menu endpoints are protected by per-action MenuActionTree permissions.
    /// When false (default), endpoints behave as before â€” only authentication is required.
    /// </summary>
    public bool EnableMenuActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Optional list of languages supported by multi-language fields (StandaloneOperationCode, MenuCode).
    /// When empty, those fields behave as plain single-language strings (no language selector on report exports).
    /// </summary>
    public List<MenuLanguageOption> Languages { get; set; } = new();

    /// <summary>
    /// When true, the menu variant's MenuPrefix/MenuPostfix are also applied to standalone menu codes
    /// during report export. When false (default), standalone codes are emitted without the prefix/postfix
    /// (matching legacy behavior). Periodic menu codes always include the prefix/postfix.
    /// </summary>
    public bool ApplyMenuPrefixPostfixToStandalones { get; set; } = false;
}

public record MenuLanguageOption(string Culture, string Label);
