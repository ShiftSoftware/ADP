using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Reusable screen composed from banked questions. Surveys reference a template by
/// <see cref="Key"/> via <c>templateRef</c>. At publish the template is expanded inline
/// into the frozen <see cref="SurveyVersion.ResolvedJson"/>.
/// </summary>
[TemporalShiftEntity]
public class ScreenTemplate : ShiftEntity<ScreenTemplate>
{
    /// <summary>Human-readable template id used by surveys in <c>templateRef</c>.</summary>
    public string Key { get; set; } = default!;

    /// <summary>Full <c>ScreenTemplateDto</c> serialized via <c>SurveySchemaSerializer.Options</c>.</summary>
    public string TemplateJson { get; set; } = default!;

    public string? Tags { get; set; }
}
