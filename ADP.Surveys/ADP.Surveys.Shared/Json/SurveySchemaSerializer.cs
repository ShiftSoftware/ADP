using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Surveys.Shared.Json;

/// <summary>
/// Canonical <see cref="JsonSerializerOptions"/> for the survey schema. All consumers
/// (API, Web, SDK host, tests) should use these so wire-format is identical everywhere.
/// </summary>
public static class SurveySchemaSerializer
{
    public static JsonSerializerOptions Options { get; } = Build();

    /// <summary>
    /// Same config as <see cref="Options"/> but with <see cref="JsonSerializerOptions.WriteIndented"/>
    /// on — convenient for logs and tests.
    /// </summary>
    public static JsonSerializerOptions Pretty { get; } = Build(indented: true);

    private static JsonSerializerOptions Build(bool indented = false) => new()
    {
        WriteIndented = indented,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Operators like ">=" must round-trip as-is, not as "\u003E=".
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Tolerate the "type" discriminator appearing after other properties —
        // we don't want authoring tools to have to reorder on save.
        AllowOutOfOrderMetadataProperties = true,
    };
}
