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
}

public record MenuLanguageOption(string Culture, string Label);
