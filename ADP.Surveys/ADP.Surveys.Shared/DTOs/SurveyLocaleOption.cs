namespace ShiftSoftware.ADP.Surveys.Shared.DTOs;

/// <summary>
/// One locale a deployment's survey builder offers: the culture code stored in
/// <see cref="SurveyDto.Locales"/> / <see cref="LocalizedString"/> keys, and the label an
/// author sees in the picker.
/// </summary>
/// <remarks>
/// Lives in Shared because BOTH sides need it: the Blazor builder reads the list to populate its
/// locale pickers, and the API options class carries the same shape. Deployments serve different
/// markets and share no common language set — one may author in en/ar/ku, another in en/ru — so
/// the catalog is deployment configuration, never a constant baked into the module.
/// </remarks>
/// <param name="Culture">Culture code, e.g. "en", "ru", "ar", "ku". Stored verbatim as the
/// <see cref="LocalizedString"/> key, so it must match what the renderer resolves against.</param>
/// <param name="Label">Author-facing label, e.g. "Русский (ru)". Shown in the builder only —
/// respondents never see it.</param>
public record SurveyLocaleOption(string Culture, string Label);
