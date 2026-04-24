using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// One row per logical survey (across all its versions). Holds the currently-editable
/// draft as a JSON blob (the full <c>SurveyDto</c>) plus metadata. Published snapshots
/// live in <see cref="SurveyVersion"/>.
///
/// Why draft-as-JSON: the authored schema is polymorphic over 14 question types, two
/// screen forms, and three logic condition shapes. Re-normalizing that into 20+ SQL
/// tables buys us no query advantage because drafts are always loaded / saved as a whole
/// unit by the builder; BI queries run against published versions and the response tables,
/// not drafts.
/// </summary>
[TemporalShiftEntity]
public class Survey : ShiftEntity<Survey>, IEntityHasUniqueHash<Survey>
{
    /// <summary>Human-readable survey name — shown in the builder's list view.</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Optional externally-facing identifier for integrations (e.g. "dealer-nps-2026").
    /// Unique across non-deleted rows — enforced by the framework's <see cref="IEntityHasUniqueHash{Entity}"/>
    /// shadow column with a filtered unique index.
    /// </summary>
    public string? IntegrationId { get; set; }

    /// <summary>
    /// The editable <c>SurveyDto</c> serialized via <c>SurveySchemaSerializer.Options</c>.
    /// Empty string while the survey has not yet been populated by the builder.
    /// </summary>
    public string DraftJson { get; set; } = "";

    /// <summary>Highest version number that has been published. Null before first publish.</summary>
    public int? PublishedVersionNumber { get; set; }

    public virtual ICollection<SurveyVersion> Versions { get; set; } = new HashSet<SurveyVersion>();
    public virtual ICollection<SurveyInstance> Instances { get; set; } = new HashSet<SurveyInstance>();

    public string? CalculateUniqueHash() =>
        string.IsNullOrWhiteSpace(IntegrationId) ? null : IntegrationId.ToUpperInvariant();
}
